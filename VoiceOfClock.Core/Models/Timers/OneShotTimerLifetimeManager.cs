using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Contracts.Models;
using static VoiceOfClock.Core.Contracts.Services.ITimeTriggerServiceBase<System.Guid>;
using System.Diagnostics;
using System.Timers;

namespace VoiceOfClock.Core.Models.Timers;

public sealed class OneShotTimerLifetimeManager 
    : IApplicationLifeCycleAware   
    , IRecipient<ActiveTimerCollectionRequestMessage>
    , IRecipient<ToastNotificationActivatedMessage>
{ 
    private readonly ITimeTriggerService _timeTriggerService;
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly IToastNotificationService _toastNotificationService;
    private readonly OneShotTimerRepository _oneShotTimerRepository;
    private readonly OneShotTimerRunningRepository _oneShotTimerRunningRepository;
    private readonly IMessenger _messenger;

    private const string TimeTriggerGroupId = "OneShot";

    public OneShotTimerLifetimeManager(
        IMessenger messenger,
        ITimeTriggerService timeTriggerService,
        ISoundContentPlayerService soundContentPlayerService,
        IToastNotificationService toastNotificationService,
        OneShotTimerRepository oneShotTimerRepository,
        OneShotTimerRunningRepository oneShotTimerRunningRepository        
        )
    {
        _messenger = messenger;
        _timeTriggerService = timeTriggerService;
        _soundContentPlayerService = soundContentPlayerService;
        _toastNotificationService = toastNotificationService;
        _oneShotTimerRepository = oneShotTimerRepository;
        _oneShotTimerRunningRepository = oneShotTimerRunningRepository;        
        _timeTriggerService.TimeTriggered += OnTimeTriggered;        
    }

    void IRecipient<ToastNotificationActivatedMessage>.Receive(ToastNotificationActivatedMessage message)
    {
        var e = message.Value;
        if (e.IsHandled) { return; }


        var args = e.Args;
        var props = e.Props;
        if (!IToastNotificationService.IsContainAction(args, TimersToastNotificationConstants.ArgumentValue_OneShot)) { return; }
        
        if (args.TryGetValue(TimersToastNotificationConstants.ArgumentKey_TimerId, out string? timerId))
        {
            Guid entityId = Guid.Parse(timerId);
            if (_playCancelMap.Remove(entityId, out var cts))
            {
                cts.Cancel();
            }

            var entity = _oneShotTimerRepository.FindById(entityId);
            _messenger.Send(new OneShotTimerCheckedMessage(entity));

            e.IsHandled = true;
        }
    }

    private void OnTimeTriggered(object? sender, TimeTriggeredEventArgs e)
    {
        if (e.GroupId != TimeTriggerGroupId) { return; }
        
        var entity = _oneShotTimerRepository.FindById(e.Id);
        Guard.IsNotNull(entity);
        if (DateTime.Now - e.TriggerTime < TimeSpan.FromSeconds(3))
        {            
            _toastNotificationService.ShowOneShotTimerToastNotification(entity);
            PlayTimerSound(entity);
        }
        else
        {
            // TODO: アプリ終了後に時間経過を検知した場合の挙動
            // 鳴動している状態の表現が必要 #
            _ = RewindTimer(entity, true);
        }
    }

    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);
    }

    void IApplicationLifeCycleAware.Resuming() { }

    void IApplicationLifeCycleAware.Suspending() { }

    
    void IRecipient<ActiveTimerCollectionRequestMessage>.Receive(ActiveTimerCollectionRequestMessage message)
    {
        foreach (var timer in GetOneShotTimers())
        {            
            if (TimerIsRunning(timer))
            {
                message.Reply(timer);
            }
        }
    }

    public OneShotTimerEntity CreateTimer(string title, TimeSpan time, SoundSourceType soundSourceType, string soundParameter)
    {
        var entity = _oneShotTimerRepository.CreateItem(new OneShotTimerEntity() 
        {
            Title = title, 
            Time = time,
            SoundSourceType = soundSourceType,
            SoundContent = soundParameter,
            Order = int.MaxValue,            
        });

        // 並び順を確実に指定する
        foreach (var (timer, index) in _oneShotTimerRepository.ReadAllItems().OrderBy(x => x.Order).Select((x, i) => (x, i)))
        {
            timer.Order = index;
            _oneShotTimerRepository.UpdateItem(timer);
        }

        return entity;
    }

    public async ValueTask DeleteTimer(OneShotTimerEntity entity)
    {
        _oneShotTimerRepository.DeleteItem(entity.Id);
        _oneShotTimerRunningRepository.DeleteItem(entity.Id);

        await _timeTriggerService.DeleteTimeTrigger(entity.Id, TimeTriggerGroupId);
    }

    public void UpdateTimer(OneShotTimerEntity entity)
    {
        _oneShotTimerRepository.UpdateItem(entity);
        if (_oneShotTimerRunningRepository.FindById(entity.Id) is not null and var runningEntity)
        {
            if (runningEntity.Time != entity.Time)
            {
                _oneShotTimerRunningRepository.DeleteItem(entity.Id);
            }
        }        
    }
    
    public void UpdateRunningTimer(OneShotTimerEntity entity, TimeSpan remainingTime)
    {
        if (_oneShotTimerRunningRepository.FindById(entity.Id) is not null and var runningEntity)
        {
            runningEntity.Time = remainingTime;
            _oneShotTimerRunningRepository.UpdateItem(runningEntity);
        }
    }

    public List<OneShotTimerEntity> GetOneShotTimers()
    {
        return _oneShotTimerRepository.ReadAllItems();
    }

    public async ValueTask StartTimer(OneShotTimerEntity entity, TimeSpan? lastRemainingTime = null)
    {
        var runningEntity = _oneShotTimerRunningRepository.FindById(entity.Id);
        TimeSpan timerDuration = lastRemainingTime ?? entity.Time;
        if (runningEntity == null)
        {
            _oneShotTimerRunningRepository.CreateItem(new OneShotTimerRunningEntity { Id = entity.Id, Time = timerDuration, IsRunning = true });
        }
        else
        {
            runningEntity.Time = timerDuration;
            runningEntity.IsRunning = true;
            _oneShotTimerRunningRepository.UpdateItem(runningEntity);
        }

        await _timeTriggerService.SetTimeTrigger(entity.Id, DateTime.Now + timerDuration, TimeTriggerGroupId);
    }

    public async ValueTask PauseTimer(OneShotTimerEntity entity)
    {
        if (_playCancelMap.Remove(entity.Id, out var cts))
        {
            cts.Cancel();
        }
        
        DateTime? triggerTime = await _timeTriggerService.GetTimeTrigger(entity.Id);
        await _timeTriggerService.DeleteTimeTrigger(entity.Id, TimeTriggerGroupId);

        if (triggerTime.HasValue)
        {
            TimeSpan remainingTime = triggerTime.Value - DateTime.Now;
            var runningEntity = _oneShotTimerRunningRepository.FindById(entity.Id);
            if (runningEntity == null)
            {
                _oneShotTimerRunningRepository.CreateItem(new OneShotTimerRunningEntity { Id = entity.Id, Time = remainingTime, IsRunning = false });
            }
            else
            {
                runningEntity.Time = remainingTime;
                runningEntity.IsRunning = false;
                _oneShotTimerRunningRepository.UpdateItem(runningEntity);
            }
        }       
    }
    
    public async ValueTask RewindTimer(OneShotTimerEntity entity, bool isRunning)
    {       
        if (isRunning)
        {
            var runningEntity = _oneShotTimerRunningRepository.FindById(entity.Id)
                ?? new OneShotTimerRunningEntity() { Id = entity.Id };

            runningEntity.Time = entity.Time;
            runningEntity.IsRunning = true;
            _oneShotTimerRunningRepository.UpdateItem(runningEntity);
            await _timeTriggerService.SetTimeTrigger(entity.Id, DateTime.Now + entity.Time, TimeTriggerGroupId);
        }
        else
        {
            _oneShotTimerRunningRepository.DeleteItem(entity.Id);
            await _timeTriggerService.DeleteTimeTrigger(entity.Id, TimeTriggerGroupId);
        }

    }



    public (bool IsRunning, DateTime TargetTime, TimeSpan RemainingTime) GetTimerRunningInfo(OneShotTimerEntity entity)
    {
        var runningInfo = _oneShotTimerRunningRepository.FindById(entity.Id);
        if (runningInfo == null) { return (false, default, entity.Time); }
        
        return _GetTimerRunningInfoInternal(runningInfo);
    }    

    private (bool IsRunning, DateTime TargetTime, TimeSpan RemainingTime) _GetTimerRunningInfoInternal(OneShotTimerRunningEntity runningInfo)
    {
        DateTime now = DateTime.Now;
        return (true, now + runningInfo.Time, runningInfo.Time);
    }

    public ValueTask<DateTime?> GetTargetTime(OneShotTimerEntity entity)
    {
        return _timeTriggerService.GetTimeTrigger(entity.Id);
    }

    public bool TimerIsRunning(OneShotTimerEntity entity)
    {
        return _oneShotTimerRunningRepository.FindById(entity.Id)?.IsRunning ?? false;
    }

    public int GetTimersCount()
    {
        return _oneShotTimerRepository.Count();
    }

    private readonly Dictionary<Guid, CancellationTokenSource> _playCancelMap = new();

    private async void PlayTimerSound(OneShotTimerEntity entity)
    {
        if (_playCancelMap.Remove(entity.Id, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }
        CancellationTokenSource cts = new CancellationTokenSource();
        _playCancelMap.Add(entity.Id, cts);
        CancellationToken ct = cts.Token;

        try
        {
            await _soundContentPlayerService.PlaySoundContentAsync(entity.SoundSourceType, entity.SoundContent, cancellationToken: ct);

        }
        catch (OperationCanceledException) { }
        finally
        {
            _playCancelMap.Remove(entity.Id);
            cts.Dispose();
        }
    }    
}
