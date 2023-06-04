using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.ViewModels;

public sealed partial class AlarmTimerPageViewModel 
    : ObservableRecipient
    , IRecipient<AlarmTimerUpdatedMessage>
    , IRecipient<NotifyAudioStartingMessage>
    , IRecipient<NotifyAudioEndedMessage>
{
    private readonly IAlarmTimerDialogService _alarmTimerDialogService;
    private readonly IStoreLisenceService _storeLisenceService;
    private readonly AlarmTimerLifetimeManager _alertTimerLifetimeManager;
    private readonly TimerSettings _timerSettings;
    private readonly ObservableCollection<AlarmTimerViewModel> _timers;
    public ReadOnlyObservableCollection<AlarmTimerViewModel> Timers { get; }

    [ObservableProperty]
    private IReadOnlyReactiveProperty<bool>? _someTimerIsActive;

    public AlarmTimerPageViewModel(
        IAlarmTimerDialogService alarmTimerDialogService
        , IStoreLisenceService storeLisenceService
        , AlarmTimerLifetimeManager alertTimerLifetimeManager
        , TimerSettings timerSettings        
        )
    {
        _alarmTimerDialogService = alarmTimerDialogService;
        _storeLisenceService = storeLisenceService;
        _alertTimerLifetimeManager = alertTimerLifetimeManager;
        _timerSettings = timerSettings;
        _timers = new ObservableCollection<AlarmTimerViewModel>(
            _alertTimerLifetimeManager.GetAlarmTimers()
            .OrderBy(x => x.Order)
            .Select(ToAlarmTimerVM)
            );
        Timers = new ReadOnlyObservableCollection<AlarmTimerViewModel>(_timers);
    }

    private AlarmTimerViewModel ToAlarmTimerVM(AlarmTimerEntity alarmTimerEntity)
    {
        return new AlarmTimerViewModel(alarmTimerEntity, 
            _timerSettings.FirstDayOfWeek, 
            _alertTimerLifetimeManager, 
            _timerSettings.IsMultiTimeZoneSupportEnabled || alarmTimerEntity.TimeZoneId != TimeZoneInfo.Local.Id,
            DeleteTimerCommand
            );
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        SomeTimerIsActive = Timers.ObserveElementProperty(x => x.IsEnabled).Select(x => Timers.Any(x => x.IsEnabled)).ToReadOnlyReactiveProperty();

        foreach (var timer in Timers)
        {
            timer.IsDisplayTimeZone = _timerSettings.IsMultiTimeZoneSupportEnabled || timer.Entity.TimeZoneId != TimeZoneInfo.Local.Id;
        }
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        SomeTimerIsActive!.Dispose();
        SomeTimerIsActive = null;
    }

    void IRecipient<AlarmTimerUpdatedMessage>.Receive(AlarmTimerUpdatedMessage message)
    {
        var sourceEntity = message.Value;
        var timerVM = Timers.FirstOrDefault(x => x.EntityId == message.Value.Id);
        if (timerVM == null) { return; }
        var destEntity = timerVM.Entity;
        if (destEntity != sourceEntity)
        {
            destEntity.Title = sourceEntity.Title;
            destEntity.IsEnabled = sourceEntity.IsEnabled;
            destEntity.SoundContent = sourceEntity.SoundContent;
            destEntity.SoundSourceType = sourceEntity.SoundSourceType;
            destEntity.TimeOfDay = sourceEntity.TimeOfDay;
            destEntity.EnabledDayOfWeeks = sourceEntity.EnabledDayOfWeeks;
            destEntity.Order = sourceEntity.Order;
            destEntity.Snooze = sourceEntity.Snooze;
            timerVM.RefrectValues();
        }
        else
        {
            timerVM.CulcTargetTime();
        }
    }

    void IRecipient<NotifyAudioStartingMessage>.Receive(NotifyAudioStartingMessage message)
    {
        var sourceEntity = message.Value;
        var timerVM = _timers.FirstOrDefault(x => x.Entity.Id == sourceEntity.Id)
            //?? (sourceEntity.Id == InstantPeriodicTimer.Entity.Id ? InstantPeriodicTimer : null)
            ;

        if (timerVM == null) { return; }

        timerVM.OnNotifyAudioStarting();
    }

    void IRecipient<NotifyAudioEndedMessage>.Receive(NotifyAudioEndedMessage message)
    {
        var sourceEntity = message.Value;
        var timerVM = _timers.FirstOrDefault(x => x.Entity.Id == sourceEntity.Id)
            //?? (sourceEntity.Id == InstantPeriodicTimer.Entity.Id ? InstantPeriodicTimer : null)
            ;

        if (timerVM == null) { return; }

        timerVM.OnNotifyAudioEnded();
        timerVM.CulcTargetTime();
    }


    [RelayCommand]
    async Task AddTimer()
    {
        if (PurchaseItemsConstants.IsTrialLimitationEnabled)
        {
            await _storeLisenceService.EnsureInitializeAsync();
            if (_storeLisenceService.IsTrial.Value && Timers.Count >= PurchaseItemsConstants.Trial_TimersLimitationCount)
            {
                var (isSuccess, error) = await _storeLisenceService.RequestPurchaiceLisenceAsync("PurchaseDialog_TitleOnInteractFromUser".Translate());
                if (!isSuccess)
                {
                    return;
                }
            }
        }

        TimeSpan now = DateTime.Now.TimeOfDay;
        var timeZones = _timerSettings.IsMultiTimeZoneSupportEnabled ? _timerSettings.GetSupportTimeZones().ToArray() : Array.Empty<TimeZoneInfo>();
        AlarmTimerDialogResult result = await _alarmTimerDialogService.ShowEditTimerAsync(
            "AlarmTimerAddDialog_Title".Translate()
            , ""
            , new TimeOnly(now.Hours, now.Minutes)
            , null
            , Enum.GetValues<DayOfWeek>()
            , _timerSettings.FirstDayOfWeek
            , SoundSourceType.System
            , SystemSoundConstants.Default
            , timeZones
            , 0
            );
        if (result.IsConfirmed)
        {
            var entity = _alertTimerLifetimeManager.CreateAlarmTimer(
                result.Title
                , result.TimeOfDay
                , result.EnabledDayOfWeeks
                , result.Snooze
                , result.SoundSourceType
                , result.SoundContent
                );

            _timers.Add(ToAlarmTimerVM(entity));
        }
    }

    [RelayCommand]
    async Task EditTimer(AlarmTimerViewModel timerVM)
    {        
        if (timerVM.NowPlayingNotifyAudio) 
        {
            if (timerVM.DismissNotificationCommand.CanExecute(null))
            {
                timerVM.DismissNotificationCommand.Execute(null);
            }
            return; 
        }

        var timeZones = _timerSettings.IsMultiTimeZoneSupportEnabled || timerVM.Entity.TimeZoneId != TimeZoneInfo.Local.Id 
            ? _timerSettings.GetSupportTimeZones().ToArray() 
            : Array.Empty<TimeZoneInfo>()
            ;
        AlarmTimerDialogResult result = await _alarmTimerDialogService.ShowEditTimerAsync(
            "AlarmTimerEditDialog_Title".Translate()
            , timerVM.Title
            , timerVM.TimeOfDay
            , timerVM.Snooze
            , timerVM.EnabledDayOfWeeks.Where(x => x.IsEnabled).Select(x => x.DayOfWeek).ToArray()
            , _timerSettings.FirstDayOfWeek
            , timerVM.SoundSourceType
            , timerVM.SoundContent
            , timeZones
            , Math.Max(Array.IndexOf(timeZones, timerVM.TimeZone), -1)
            );
        if (result.IsConfirmed)
        {
            timerVM.IsEnabled = true;
            timerVM.Title = result.Title;
            timerVM.Snooze = result.Snooze;
            timerVM.TimeOfDay = result.TimeOfDay;
            timerVM.SoundSourceType = result.SoundSourceType;
            timerVM.SoundContent = result.SoundContent;
            timerVM.TimeZone = result.TimeZone ?? timerVM.TimeZone;

            foreach (var dayOfWeekVM in timerVM.EnabledDayOfWeeks)
            {
                dayOfWeekVM.IsEnabled = result.EnabledDayOfWeeks.Any(x => x == dayOfWeekVM.DayOfWeek);
            }

            timerVM.RefrectBackValues();
        }
    }


    [RelayCommand]
    void DeleteTimer(AlarmTimerViewModel timerVM)
    {
        _alertTimerLifetimeManager.DeleteAlarmTimer(timerVM.Entity);
        _timers.Remove(timerVM);
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
