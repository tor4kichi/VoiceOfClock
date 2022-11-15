using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using I18NPortable;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Services;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;


public interface IAlarmTimerDialogService
{
    Task<AlarmTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle, TimeOnly dayOfTime, TimeSpan? snooze, IEnumerable<DayOfWeek> enabledDayOfWeeks, DayOfWeek firstDayOfWeek, SoundSourceType soundSourceType, string soundContent);
}

public sealed class AlarmTimerDialogResult
{
    public bool IsConfirmed { get; init; }
    public string Title { get; init; } = String.Empty;
    public TimeOnly TimeOfDay { get; init; }
    public TimeSpan? Snooze { get; init; }
    public DayOfWeek[] EnabledDayOfWeeks { get; init; } = Array.Empty<DayOfWeek>();
    public SoundSourceType SoundSourceType { get; init; }
    public string SoundContent { get; init; } = string.Empty;
}

public sealed partial class AlarmTimerPageViewModel : ObservableRecipient
{
    private readonly AlarmTimerLifetimeManager _alertTimerLifetimeManager;
    private readonly IAlarmTimerDialogService _alarmTimerDialogService;
    private readonly TimerSettings _timerSettings;
    private readonly StoreLisenceService _storeLisenceService;

    public AlarmTimerPageViewModel(
        AlarmTimerLifetimeManager alertTimerLifetimeManager
        , IAlarmTimerDialogService alarmTimerDialogService
        , TimerSettings timerSettings
        , StoreLisenceService storeLisenceService
        )
    {
        _alertTimerLifetimeManager = alertTimerLifetimeManager;
        _alarmTimerDialogService = alarmTimerDialogService;
        _timerSettings = timerSettings;
        _storeLisenceService = storeLisenceService;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        Timers = _alertTimerLifetimeManager.Timers.ToReadOnlyReactiveCollection(ToAlarmTimerVM, disposeElement: true);
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        Timers!.Dispose();
        Timers = null;
    }

    private AlarmTimerViewModel ToAlarmTimerVM(AlarmTimerRunningInfo runningInfo)
    {
        return new AlarmTimerViewModel(runningInfo, _timerSettings.FirstDayOfWeek, DeleteTimer);
    }    

    [ObservableProperty]
    private ReadOnlyReactiveCollection<AlarmTimerViewModel>? _timers;

    [RelayCommand]
    async Task AddTimer()
    {
        if (PurchaseItemsConstants.IsTrialLimitationEnabled)
        {
            await _storeLisenceService.EnsureInitializeAsync();
            if (_storeLisenceService.IsTrial.Value && _alertTimerLifetimeManager.Timers.Count >= PurchaseItemsConstants.Trial_TimersLimitationCount)
            {
                var (isSuccess, error) = await _storeLisenceService.RequestPurchaiceLisenceAsync("PurchaseDialog_TitleOnInteractFromUser".Translate());
                if (!isSuccess)
                {
                    return;
                }
            }
        }

        AlarmTimerDialogResult result = await _alarmTimerDialogService.ShowEditTimerAsync(
            "AlarmTimerAddDialog_Title".Translate()
            , ""
            , TimeOnly.FromDateTime(DateTime.Now)
            , null
            , Enum.GetValues<DayOfWeek>()
            , _timerSettings.FirstDayOfWeek
            , SoundSourceType.System
            , SystemSoundConstants.Default
            );
        if (result.IsConfirmed)
        {
            _alertTimerLifetimeManager.CreateAlarmTimer(
                result.Title
                , result.TimeOfDay
                , result.EnabledDayOfWeeks
                , result.Snooze
                , result.SoundSourceType
                , result.SoundContent
                );
        }
    }

    [RelayCommand]
    async Task EditTimer(AlarmTimerViewModel timerVM)
    {
        AlarmTimerRunningInfo runningInfo = timerVM.RunningInfo;
        AlarmTimerDialogResult result = await _alarmTimerDialogService.ShowEditTimerAsync(
            "AlarmTimerEditDialog_Title".Translate()
            , runningInfo.Title
            , runningInfo.TimeOfDay
            , runningInfo.Snooze
            , runningInfo.EnabledDayOfWeeks
            , _timerSettings.FirstDayOfWeek
            , runningInfo.SoundSourceType
            , runningInfo.SoundContent
            );
        if (result.IsConfirmed)
        {
            using (runningInfo.DeferUpdate())
            {
                runningInfo.IsEnabled = true;
                runningInfo.Title = result.Title;
                runningInfo.Snooze = result.Snooze;
                runningInfo.TimeOfDay = result.TimeOfDay;
                runningInfo.EnabledDayOfWeeks = result.EnabledDayOfWeeks;
                runningInfo.SoundSourceType = result.SoundSourceType;
                runningInfo.SoundContent = result.SoundContent;
                timerVM.RefrectValues();
            }
        }
    }


    [RelayCommand]
    void DeleteTimer(AlarmTimerViewModel timerVM)
    {
        _alertTimerLifetimeManager.DeleteAlarmTimer(timerVM.RunningInfo);        
    }

    [ObservableProperty]
    private bool _nowEditting;

    [RelayCommand]
    void DeleteToggle()
    {
        if (_timers == null) { return; }

        NowEditting = !NowEditting;

        foreach (var timerVM in _timers)
        {
            timerVM.IsEditting = NowEditting;
        }
    }
}
