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
using VoiceOfClock.Contracts.UseCases;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;



public sealed partial class AlarmTimerPageViewModel 
    : ObservableRecipient
    , IRecipient<AlarmTimerValueChangedMessage>
{
    private readonly AlarmTimerLifetimeManager _alertTimerLifetimeManager;
    private readonly IAlarmTimerDialogService _alarmTimerDialogService;
    private readonly TimerSettings _timerSettings;
    private readonly IStoreLisenceService _storeLisenceService;

    private readonly ObservableCollection<AlarmTimerViewModel> _timers;

    public ReadOnlyObservableCollection<AlarmTimerViewModel> Timers { get; }

    [ObservableProperty]
    private IReadOnlyReactiveProperty<bool>? _someTimerIsActive;

    public AlarmTimerPageViewModel(
        AlarmTimerLifetimeManager alertTimerLifetimeManager
        , IAlarmTimerDialogService alarmTimerDialogService
        , TimerSettings timerSettings
        , IStoreLisenceService storeLisenceService
        )
    {
        _alertTimerLifetimeManager = alertTimerLifetimeManager;
        _alarmTimerDialogService = alarmTimerDialogService;
        _timerSettings = timerSettings;
        _storeLisenceService = storeLisenceService;
        _timers = new ObservableCollection<AlarmTimerViewModel>(_alertTimerLifetimeManager.GetAlarmTimers().OrderBy(x => x.Order).Select(ToAlarmTimerVM));
        Timers = new ReadOnlyObservableCollection<AlarmTimerViewModel>(_timers);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        SomeTimerIsActive = Timers.ObserveElementProperty(x => x.IsEnabled).Select(x => Timers.Any(x => x.IsEnabled)).ToReadOnlyReactiveProperty();
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        SomeTimerIsActive!.Dispose();
        SomeTimerIsActive = null;
    }

    private AlarmTimerViewModel ToAlarmTimerVM(AlarmTimerEntity alarmTimerEntity)
    {
        return new AlarmTimerViewModel(alarmTimerEntity, _timerSettings.FirstDayOfWeek, _alertTimerLifetimeManager, DeleteTimer);
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
        AlarmTimerDialogResult result = await _alarmTimerDialogService.ShowEditTimerAsync(
            "AlarmTimerAddDialog_Title".Translate()
            , ""
            , new TimeOnly(now.Hours, now.Minutes)
            , null
            , Enum.GetValues<DayOfWeek>()
            , _timerSettings.FirstDayOfWeek
            , SoundSourceType.System
            , SystemSoundConstants.Default
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
        AlarmTimerDialogResult result = await _alarmTimerDialogService.ShowEditTimerAsync(
            "AlarmTimerEditDialog_Title".Translate()
            , timerVM.Title
            , timerVM.TimeOfDay
            , timerVM.Snooze
            , timerVM.EnabledDayOfWeeks.Where(x => x.IsEnabled).Select(x => x.DayOfWeek).ToArray()
            , _timerSettings.FirstDayOfWeek
            , timerVM.SoundSourceType
            , timerVM.SoundContent
            );
        if (result.IsConfirmed)
        {
            timerVM.IsEnabled = true;
            timerVM.Title = result.Title;
            timerVM.Snooze = result.Snooze;
            timerVM.TimeOfDay = result.TimeOfDay;
            timerVM.SoundSourceType = result.SoundSourceType;
            timerVM.SoundContent = result.SoundContent;

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

    void IRecipient<AlarmTimerValueChangedMessage>.Receive(AlarmTimerValueChangedMessage message)
    {        
        var timerVM = Timers.FirstOrDefault(x => x.EntityId == message.Value.Id);
        if (timerVM == null) { return; }
        if (timerVM.Entity == message.Value) { return; }

        timerVM.RefrectValues();
    }
}
