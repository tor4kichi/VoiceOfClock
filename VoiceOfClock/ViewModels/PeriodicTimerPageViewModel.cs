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
using VoiceOfClock.Services;
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
    private readonly PeriodicTimerLifetimeManager _timerLifetimeManager;
    private readonly StoreLisenceService _storeLisenceService;

    [ObservableProperty]
    private ReadOnlyReactiveCollection<PeriodicTimerViewModel>? _timers;

    [ObservableProperty]
    private PeriodicTimerViewModel? _instantPeriodicTimer;

    public TimerSettings TimerSettings { get; }


    public PeriodicTimerPageViewModel(
        IMessenger messenger
        , IPeriodicTimerDialogService dialogService
        , PeriodicTimerLifetimeManager timerLifetimeManager
        , TimerSettings timerSettings
        , StoreLisenceService storeLisenceService
        )
        : base(messenger)
    {
        _dialogService = dialogService;
        _timerLifetimeManager = timerLifetimeManager;
        TimerSettings = timerSettings;
        _storeLisenceService = storeLisenceService;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        
        Timers = _timerLifetimeManager.Timers.ToReadOnlyReactiveCollection(x => new PeriodicTimerViewModel(x, DeleteTimerCommand, TimerSettings.FirstDayOfWeek));
        InstantPeriodicTimer = new PeriodicTimerViewModel(_timerLifetimeManager.InstantPeriodicTimer, DeleteTimerCommand, TimerSettings.FirstDayOfWeek);
    }

    protected override void OnDeactivated()
    {
        foreach (var timer in Timers ?? Enumerable.Empty<PeriodicTimerViewModel>())
        {
            (timer as IDisposable)?.Dispose();
        }
        Timers = null;

        (InstantPeriodicTimer as IDisposable)?.Dispose();
        InstantPeriodicTimer = null;

        base.OnDeactivated();
    }

    [RelayCommand]
    async Task AddTimer()
    {
        if (PurchaseItemsConstants.IsTrialLimitationEnabled)
        {
            await _storeLisenceService.EnsureInitializeAsync();
            if (_storeLisenceService.IsTrial.Value && _timerLifetimeManager.Timers.Count >= PurchaseItemsConstants.Trial_TimersLimitationCount)
            {
                var (isSuccess, error) = await _storeLisenceService.RequestPurchaiceLisenceAsync("PurchaseDialog_TitleOnInteractFromUser".Translate());
                if (!isSuccess)
                {
                    return;
                }
            }
        }

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
                timerVM.IsEnabled = timerInfo.IsEnabled = true;
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
        if (Timers is null) { return; }

        NowEditting = !NowEditting;
        foreach (var timer in Timers)
        {
            timer.IsEditting = NowEditting;
        }
    }

    [RelayCommand]
    void StartImmidiateTimer(TimeSpan intervalTime)
    {
        if (InstantPeriodicTimer is null) { return; }

        _timerLifetimeManager.StartInstantPeriodicTimer(intervalTime);            
        InstantPeriodicTimer.IsEnabled = true;            
    }

    [RelayCommand]
    void StopImmidiateTimer()
    {
        if (InstantPeriodicTimer is null) { return; }

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
        if (Timers is null) { return; }

        Timers.FirstOrDefault(x => x.PeriodicTimerRunningInfo._entity.Id == message.Value.Id)?.RefrectValue();
    }
}
