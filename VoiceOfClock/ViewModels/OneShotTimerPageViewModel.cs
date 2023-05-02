using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using Microsoft.UI.Dispatching;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.ViewModels;

public sealed partial class OneShotTimerPageViewModel 
    : ObservableRecipient
    , IRecipient<OneShotTimerCheckedMessage>
{
    private readonly OneShotTimerLifetimeManager _oneShotTimerLifetimeManager;
    private readonly IOneShotTimerDialogService _oneShotTimerDialogService;
    private readonly IStoreLisenceService _storeLisenceService;
    private readonly DispatcherQueueTimer _timer;

    private ObservableCollection<OneShotTimerViewModel> _timers;
    public ReadOnlyObservableCollection<OneShotTimerViewModel> Timers { get; }
    
    [ObservableProperty]
    private IReadOnlyReactiveProperty<bool>? _someTimerIsActive;

    public OneShotTimerPageViewModel(
        IOneShotTimerDialogService oneShotTimerDialogService
        , IStoreLisenceService storeLisenceService
        , OneShotTimerLifetimeManager oneShotTimerLifetimeManager
        )
    {
        _oneShotTimerDialogService = oneShotTimerDialogService;
        _storeLisenceService = storeLisenceService;
        _oneShotTimerLifetimeManager = oneShotTimerLifetimeManager;
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Tick += OnTimerTick;
        _timer.Interval = TimeSpan.FromSeconds(1d / 6);
        _timers = new ObservableCollection<OneShotTimerViewModel>(
            _oneShotTimerLifetimeManager.GetOneShotTimers()
            .OrderBy(x => x.Order)
            .Select(ToTimerViewModel)
            );
        Timers = new(_timers);
    }

    private OneShotTimerViewModel ToTimerViewModel(OneShotTimerEntity entity)
    {
        return new OneShotTimerViewModel(entity, _oneShotTimerLifetimeManager, Messenger, DeleteTimerCommand);
    }

    protected override void OnActivated()
    {
        SomeTimerIsActive = Timers.ObserveElementProperty(x => x.IsRunning).Select(x => Timers.Any(x => x.IsRunning)).ToReadOnlyReactiveProperty();
        _timer.Start();
        base.OnActivated();
    }

    protected override void OnDeactivated()
    {
        _timer.Stop();
        SomeTimerIsActive!.Dispose();
        SomeTimerIsActive = null;
        base.OnDeactivated();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        foreach (var timerVM in Timers)
        {
            if (timerVM.IsRunning)
            {
                timerVM.UpdateRemainingTime();
            }
        }
    }

    void IRecipient<OneShotTimerCheckedMessage>.Receive(OneShotTimerCheckedMessage message)
    {
        var sourceEntity = message.Value;
        var timerVM = _timers.FirstOrDefault(x => x.Entity.Id == sourceEntity.Id);
        Guard.IsNotNull(timerVM);

        timerVM.IsRunning = false;

        var destEntity = timerVM.Entity;
        destEntity.Title = sourceEntity.Title;
        destEntity.Time = sourceEntity.Time;
        destEntity.Order = sourceEntity.Order;
        destEntity.SoundContent = sourceEntity.SoundContent;
        destEntity.SoundSourceType = sourceEntity.SoundSourceType;

        timerVM.RefrectValues();
        timerVM.UpdateRemainingTime();
    }


    [RelayCommand]
    async Task AddTimer()
    {
        if (PurchaseItemsConstants.IsTrialLimitationEnabled)
        {
            await _storeLisenceService.EnsureInitializeAsync();
            if (_storeLisenceService.IsTrial.Value && _oneShotTimerLifetimeManager.GetTimersCount() >= PurchaseItemsConstants.Trial_TimersLimitationCount)
            {
                var (isSuccess, error) = await _storeLisenceService.RequestPurchaiceLisenceAsync("PurchaseDialog_TitleOnInteractFromUser".Translate());
                if (!isSuccess)
                {
                    return;
                }
            }
        }

        var result = await _oneShotTimerDialogService.ShowEditTimerAsync("OneShotTimerAddDialog_Title".Translate(), "", TimeSpan.FromMinutes(3), SoundSourceType.System, WindowsNotificationSoundType.Default.ToString());
        if (result.IsConfirmed)
        {
            var newEntity = _oneShotTimerLifetimeManager.CreateTimer(result.Title, result.Time, result.SoundSourceType, result.SoundParameter);
            _timers.Add(ToTimerViewModel(newEntity));
        }
    }

    [ObservableProperty]
    private bool _nowEditting;

    [RelayCommand]
    async Task EditTimer(OneShotTimerViewModel timerVM)
    {
        if (timerVM.IsRunning) { return; }

        var result = await _oneShotTimerDialogService.ShowEditTimerAsync("OneShotTimerEditDialog_Title".Translate(), timerVM.Title, timerVM.Time, timerVM.Entity.SoundSourceType, timerVM.Entity.SoundContent);
        if (result.IsConfirmed)
        {
            var entity = timerVM.Entity;            
            entity.Title = result.Title;
            entity.Time = result.Time;                
            entity.SoundSourceType = result.SoundSourceType;
            entity.SoundContent = result.SoundParameter;

            timerVM.RefrectValues();
            _oneShotTimerLifetimeManager.UpdateTimer(timerVM.Entity);
            
        }
    }

    [RelayCommand]
    void DeleteTimer(OneShotTimerViewModel timerVM)
    {
        _oneShotTimerLifetimeManager.DeleteTimer(timerVM.Entity);
        _timers.Remove(timerVM);
    }
    

    [RelayCommand]
    void DeleteToggle()
    {
        NowEditting = !NowEditting;
        foreach (var timer in Timers ?? Enumerable.Empty<OneShotTimerViewModel>())
        {
            timer.IsEditting = NowEditting;
        }
    }

}
