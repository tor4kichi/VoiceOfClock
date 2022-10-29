using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;

public interface IPeriodicTimerDialogService
{
    Task<PeriodicTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime, IEnumerable<DayOfWeek> enabledDayOfWeeks, DayOfWeek firstDayOfWeek);
}

public sealed class PeriodicTimerDialogResult
{
    public bool IsConfirmed { get; init; }
    public string Title { get; init; } = String.Empty;
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public TimeSpan IntervalTime { get; init; }        
    public DayOfWeek[] EnabledDayOfWeeks { get; init; } = Array.Empty<DayOfWeek>();
}

// ページを開いていなくても時刻読み上げは動作し続けることを前提に
// ページの表示状態を管理する
public sealed partial class PeriodicTimerPageViewModel : ObservableRecipient,
    IRecipient<PeriodicTimerUpdated>
{
    private readonly IPeriodicTimerDialogService _dialogService;
    private readonly TimerLifetimeManager _timerLifetimeManager;
    public ReadOnlyReactiveCollection<PeriodicTimerViewModel> Timers { get; }
    public TimerSettings TimerSettings { get; }
    public PeriodicTimerViewModel InstantPeriodicTimer { get; }


    public PeriodicTimerPageViewModel(
        IMessenger messenger,
        IPeriodicTimerDialogService dialogService,
        TimerLifetimeManager timerLifetimeManager,
        TimerSettings timerSettings
        )
        : base(messenger)
    {
        _dialogService = dialogService;
        _timerLifetimeManager = timerLifetimeManager;
        TimerSettings = timerSettings;
        
        Timers = timerLifetimeManager.PeriodicTimers.ToReadOnlyReactiveCollection(x => new PeriodicTimerViewModel(x, DeleteTimerCommand, TimerSettings.FirstDayOfWeek));
        InstantPeriodicTimer = new PeriodicTimerViewModel(timerLifetimeManager.InstantPeriodicTimer, DeleteTimerCommand, TimerSettings.FirstDayOfWeek);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        InstantPeriodicTimer.RefrectValue();
        foreach (var timer in Timers)
        {
            timer.RefrectValue();
        }            
    }

    [RelayCommand]
    async Task AddTimer()
    {
        var result = await _dialogService.ShowEditTimerAsync("PeriodicTimerAddDialog_Title".Translate(), "", TimeSpan.Zero, TimeSpan.FromHours(1), TimeSpan.FromMinutes(5), Enum.GetValues<DayOfWeek>(), TimerSettings.FirstDayOfWeek);
        if (result?.IsConfirmed is true)
        {
            _timerLifetimeManager.CreatePeriodicTimer(
                result.Title
                , result.StartTime
                , result.EndTime
                , result.IntervalTime
                , result.EnabledDayOfWeeks
                );
        }
    }

    [RelayCommand]
    void DeleteTimer(PeriodicTimerViewModel timerVM)
    {
        if (timerVM.IsRemovable is false) { return; }

        _timerLifetimeManager.DeletePeriodicTimer(timerVM.PeriodicTimerRunningInfo);
    }

    [RelayCommand]
    async Task EditTimer(PeriodicTimerViewModel timerVM)
    {
        var result = await _dialogService.ShowEditTimerAsync("PeriodicTimerEditDialog_Title".Translate(), timerVM.Title, timerVM.StartTime, timerVM.EndTime, timerVM.IntervalTime, timerVM.EnabledDayOfWeeks.Where(x => x.IsEnabled).Select(x => x.DayOfWeek), TimerSettings.FirstDayOfWeek);
        if (result?.IsConfirmed is true)
        {
            var timerInfo = timerVM.PeriodicTimerRunningInfo;
            using (timerInfo.DeferUpdate())
            {
                timerVM.StartTime = timerInfo.StartTime = result.StartTime;
                timerVM.EndTime  = timerInfo.EndTime = result.EndTime;
                timerVM.IntervalTime = timerInfo.IntervalTime = result.IntervalTime;
                timerVM.Title  = timerInfo.Title = result.Title;
                foreach (var enabledDayOfWeekVM in timerVM.EnabledDayOfWeeks)
                {
                    enabledDayOfWeekVM.IsEnabled = result.EnabledDayOfWeeks.Contains(enabledDayOfWeekVM.DayOfWeek);
                }

                timerInfo.EnabledDayOfWeeks = result.EnabledDayOfWeeks;
            }
        }
    }

           
    private bool _nowEditting;
    public bool NowEditting
    {
        get => _nowEditting;
        private set => SetProperty(ref _nowEditting, value);
    }


    [RelayCommand]
    void DeleteToggle()
    {
        NowEditting = !NowEditting;
        foreach (var timer in Timers)
        {
            timer.IsEditting = NowEditting;
        }
    }

    [RelayCommand]
    void StartImmidiateTimer(TimeSpan intervalTime)
    {
        _timerLifetimeManager.StartInstantPeriodicTimer(intervalTime);            
        InstantPeriodicTimer.IsEnabled = true;            
    }

    [RelayCommand]
    void StopImmidiateTimer()
    {
        _timerLifetimeManager.StopInstantPeriodicTimer();
        InstantPeriodicTimer.IsEnabled = false;
    }

    public static string ConvertDateTime(DateTime dateTime)
    {
        var d = dateTime;
        return "DateTime_Month_Day_Hour_Minite".Translate(d.Minute, d.Hour, d.Day, d.Month);
    }

    public static string ConvertElapsedTime(TimeSpan timeSpan)
    {
        if (timeSpan.Days >= 1)
        {
            return "ElapsedTime_Days_Hours_Minutes_Seconds".Translate(timeSpan.Seconds, timeSpan.Minutes, timeSpan.Hours, timeSpan.Days);
        }
        else if (timeSpan.Hours >= 1)
        {
            return "ElapsedTime_Hours_Minutes_Seconds".Translate(timeSpan.Seconds, timeSpan.Minutes, timeSpan.Hours);
        }
        else if (timeSpan.Minutes >= 1)
        {
            return "ElapsedTime_Minutes_Seconds".Translate(timeSpan.Seconds, timeSpan.Minutes);
        }
        else
        {
            return "ElapsedTime_Seconds".Translate(timeSpan.Seconds);
        }            
    }

    void IRecipient<PeriodicTimerUpdated>.Receive(PeriodicTimerUpdated message)
    {
        Timers.FirstOrDefault(x => x.PeriodicTimerRunningInfo._entity.Id == message.Value.Id)?.RefrectValue();
    }
}
