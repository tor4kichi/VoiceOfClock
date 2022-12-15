using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using Microsoft.UI.Dispatching;
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
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;
using VoiceOfClock.Core.Contracts.Models;

namespace VoiceOfClock.ViewModels;

public sealed partial class PeriodicTimerPageViewModel 
    : ObservableRecipient
    , IRecipient<PeriodicTimerUpdatedMessage>
    , IRecipient<PeriodicTimerProgressPeriodMessage>
{
    private readonly IPeriodicTimerDialogService _dialogService;
    private readonly IStoreLisenceService _storeLisenceService;
    private readonly PeriodicTimerLifetimeManager _timerLifetimeManager;
    public TimerSettings TimerSettings { get; }
    private readonly DispatcherQueueTimer _dispatcherQueueTimer;
    private readonly ObservableCollection<PeriodicTimerViewModel> _timers;
    public ReadOnlyObservableCollection<PeriodicTimerViewModel> Timers { get; }
    public PeriodicTimerViewModel InstantPeriodicTimer { get; }

    [ObservableProperty]
    private IReadOnlyReactiveProperty<bool>? _someTimerIsActive;

    private const double PeriodicTimerUpdateFrequencyOnSeconds = 1.0d / 5; 

    public PeriodicTimerPageViewModel(
        IMessenger messenger
        , IPeriodicTimerDialogService dialogService
        , IStoreLisenceService storeLisenceService
        , PeriodicTimerLifetimeManager timerLifetimeManager
        , TimerSettings timerSettings
        )
        : base(messenger)
    {
        _dialogService = dialogService;
        _storeLisenceService = storeLisenceService;
        _timerLifetimeManager = timerLifetimeManager;
        TimerSettings = timerSettings;
        _dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(PeriodicTimerUpdateFrequencyOnSeconds);
        _dispatcherQueueTimer.IsRepeating = true;
        _timers = new ObservableCollection<PeriodicTimerViewModel>(_timerLifetimeManager.GetTimers().OrderBy(x => x.Order).Select(ToTimerViewModel));
        Timers = new(_timers);
        InstantPeriodicTimer = ToTimerViewModel(_timerLifetimeManager.InstantPeriodicTimer);
    }    

    private PeriodicTimerViewModel ToTimerViewModel(PeriodicTimerEntity entity)
    {
        return new PeriodicTimerViewModel(entity, _timerLifetimeManager, DeleteTimerCommand, TimerSettings.FirstDayOfWeek);
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        _dispatcherQueueTimer.Tick += OnTimerTick;
        _dispatcherQueueTimer.Start();

        SomeTimerIsActive = 
            new[] 
            {
                InstantPeriodicTimer.ObserveProperty(x => x.IsEnabled).ToUnit(),
                Timers.ObserveElementProperty(x => x.IsEnabled).ToUnit(),
            }
            .Merge().Select(x => InstantPeriodicTimer.Entity.IsEnabled || Timers.Any(x => x.IsEnabled)).ToReadOnlyReactiveProperty();
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

        _dispatcherQueueTimer.Stop();
        _dispatcherQueueTimer.Tick -= OnTimerTick;

        SomeTimerIsActive!.Dispose();
        SomeTimerIsActive = null;        
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (InstantPeriodicTimer.IsEnabled)
        {
            InstantPeriodicTimer.UpdateElapsedTime();
        }

        foreach (var timerVM in _timers)
        {
            if (timerVM.IsEnabled && timerVM.IsInsidePeriod)
            {
                timerVM.UpdateElapsedTime();
            }
        }
    }

    void IRecipient<PeriodicTimerUpdatedMessage>.Receive(PeriodicTimerUpdatedMessage message)
    {
        var sourceEntity = message.Value;
        var timerVM = _timers.FirstOrDefault(x => x.Entity.Id == sourceEntity.Id)
            ?? (sourceEntity.Id == InstantPeriodicTimer.Entity.Id ? InstantPeriodicTimer : null)
            ;        
        if (timerVM == null) { return; }
        if (timerVM.Entity != sourceEntity) 
        {
            var destEntity = timerVM.Entity;
            destEntity.Title = sourceEntity.Title;
            destEntity.IntervalTime = sourceEntity.IntervalTime;
            destEntity.StartTime = sourceEntity.StartTime;
            destEntity.EndTime = sourceEntity.EndTime;
            destEntity.Order = sourceEntity.Order;
            destEntity.EnabledDayOfWeeks = sourceEntity.EnabledDayOfWeeks;
            destEntity.IsEnabled = sourceEntity.IsEnabled;

            timerVM.RefrectValues();
        }

        timerVM.CulcNextTime();
    }

    void IRecipient<PeriodicTimerProgressPeriodMessage>.Receive(PeriodicTimerProgressPeriodMessage message)
    {
        var destEntity = message.Value;
        var timerVM = _timers.FirstOrDefault(x => x.Entity.Id == destEntity.Id)
            ?? (destEntity.Id == InstantPeriodicTimer.Entity.Id ? InstantPeriodicTimer : null)
            ;
        
        timerVM?.CulcNextTime();
    }

    [RelayCommand]
    async Task AddTimer()
    {
        if (PurchaseItemsConstants.IsTrialLimitationEnabled)
        {
            await _storeLisenceService.EnsureInitializeAsync();
            if (_storeLisenceService.IsTrial.Value && _timerLifetimeManager.GetTimerCount() >= PurchaseItemsConstants.Trial_TimersLimitationCount)
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
            var newEntity = _timerLifetimeManager.CreatePeriodicTimer(
                result.Title
                , result.StartTime
                , result.EndTime
                , result.IntervalTime
                , result.EnabledDayOfWeeks
                );

            _timers.Add(ToTimerViewModel(newEntity));
        }
    }

    [RelayCommand]
    void DeleteTimer(PeriodicTimerViewModel timerVM)
    {
        if (timerVM.IsRemovable is false) { return; }

        _timerLifetimeManager.DeletePeriodicTimer(timerVM.Entity);
        _timers.Remove(timerVM);
    }

    [RelayCommand]
    async Task EditTimer(PeriodicTimerViewModel timerVM)
    {
        var result = await _dialogService.ShowEditTimerAsync("PeriodicTimerEditDialog_Title".Translate(), timerVM.Title, timerVM.StartTime, timerVM.EndTime, timerVM.IntervalTime, timerVM.EnabledDayOfWeeks.Where(x => x.IsEnabled).Select(x => x.DayOfWeek), TimerSettings.FirstDayOfWeek);
        if (result?.IsConfirmed is true)
        {
            var entity = timerVM.Entity;            
            entity.IsEnabled = true;
            entity.StartTime = result.StartTime;
            entity.EndTime = result.EndTime;
            entity.IntervalTime = result.IntervalTime;
            entity.Title = result.Title;                
            entity.EnabledDayOfWeeks = result.EnabledDayOfWeeks;

            timerVM.RefrectValues();
            timerVM.CulcNextTime();

            _timerLifetimeManager.UpdatePeriodicTimer(entity);
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

    [RelayCommand(CanExecute = nameof(CanExecuteStartImmidiateTimer))]
    void StartImmidiateTimer(TimeSpan intervalTime)
    {
        if (InstantPeriodicTimer is null) { return; }

        _timerLifetimeManager.StartInstantPeriodicTimer(intervalTime);
        InstantPeriodicTimer.CulcNextTime();
        InstantPeriodicTimer.IsEnabled = true;            
    }

    bool CanExecuteStartImmidiateTimer(TimeSpan intervalTime)
    {
        return intervalTime >= TimeSpan.FromSeconds(1);
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

}
