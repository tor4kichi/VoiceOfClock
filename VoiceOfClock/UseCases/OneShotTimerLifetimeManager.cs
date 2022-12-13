using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using I18NPortable;
using LiteDB;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Contracts.UseCases;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.Services.SoundPlayer;
using VoiceOfClock.ViewModels;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;

namespace VoiceOfClock.UseCases;

public sealed class OneShotTimerLifetimeManager 
    : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
    , IToastActivationAware     
{ 
    private readonly ITimeTriggerService _timeTriggerService;
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly OneShotTimerRepository _oneShotTimerRepository;
    private readonly OneShotTimerRunningRepository _oneShotTimerRunningRepository;
    private readonly IMessenger _messenger;

    private const string TimeTriggerGroupId = nameof(OneShotTimerLifetimeManager);

    public OneShotTimerLifetimeManager(
        ITimeTriggerService timeTriggerService,
        ISoundContentPlayerService soundContentPlayerService,
        OneShotTimerRepository oneShotTimerRepository,
        OneShotTimerRunningRepository oneShotTimerRunningRepository,
        IMessenger messenger
        )
    {
        _timeTriggerService = timeTriggerService;
        _soundContentPlayerService = soundContentPlayerService;
        _oneShotTimerRepository = oneShotTimerRepository;
        _oneShotTimerRunningRepository = oneShotTimerRunningRepository;
        _messenger = messenger;
        _timeTriggerService.TimeTriggered += _timeTriggerService_TimeTriggered;
    }

    private void _timeTriggerService_TimeTriggered(object? sender, TimeTriggeredEventArgs e)
    {
        if (e.GroupId != TimeTriggerGroupId) { return; }
        if (!Guid.TryParse(e.Id, out Guid timerId)) { return; }

        var entity = _oneShotTimerRepository.FindById(timerId);

        ShowOneShotTimerToastNotification(entity);
        PlayTimerSound(entity);

    }

    void IApplicationLifeCycleAware.Initialize()
    {
        var timers = _oneShotTimerRepository.ReadAllItems().OrderBy(x => x.Order);

        if (SystemInformation.Instance.IsFirstRun)
        {
            CreateTimer("OneShotTimer_TemporaryTitle".Translate(1), TimeSpan.FromMinutes(3), SoundSourceType.System, WindowsNotificationSoundType.Reminder.ToString());
        }
    }

    void IApplicationLifeCycleAware.Resuming()
    {
        
    }

    void IApplicationLifeCycleAware.Suspending()
    {
        
    }



    void IRecipient<ActiveTimerCollectionRequestMessage>.Receive(ActiveTimerCollectionRequestMessage message)
    {
        foreach (var timer in GetOneShotTimers())
        {            
            if (_oneShotTimerRunningRepository.FindById(timer.Id) is not null and var runningTimer)
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

        await _timeTriggerService.DeleteTimeTrigger(entity.Id.ToString(), TimeTriggerGroupId);
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
            _oneShotTimerRunningRepository.CreateItem(new OneShotTimerRunningEntity { Id = entity.Id, Time = timerDuration });
        }
        else
        {
            runningEntity.Time = timerDuration;
            _oneShotTimerRunningRepository.UpdateItem(runningEntity);
        }

        await _timeTriggerService.SetTimeTrigger(entity.Id.ToString(), DateTime.Now + timerDuration, TimeTriggerGroupId);
    }

    public async ValueTask PauseTimer(OneShotTimerEntity entity)
    {
        if (_playCancelMap.Remove(entity.Id, out var cts))
        {
            cts.Cancel();
        }
        
        DateTime? triggerTime = await _timeTriggerService.GetTimeTrigger(entity.Id.ToString());
        await _timeTriggerService.DeleteTimeTrigger(entity.Id.ToString(), TimeTriggerGroupId);

        if (triggerTime.HasValue)
        {
            TimeSpan remainingTime = triggerTime.Value - DateTime.Now;
            var runningEntity = _oneShotTimerRunningRepository.FindById(entity.Id);
            if (runningEntity == null)
            {
                _oneShotTimerRunningRepository.CreateItem(new OneShotTimerRunningEntity { Id = entity.Id, Time = remainingTime });
            }
            else
            {
                runningEntity.Time = remainingTime;
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
            _oneShotTimerRunningRepository.UpdateItem(runningEntity);
            await _timeTriggerService.SetTimeTrigger(entity.Id.ToString(), DateTime.Now + entity.Time, TimeTriggerGroupId);
        }
        else
        {
            _oneShotTimerRunningRepository.DeleteItem(entity.Id);
            await _timeTriggerService.DeleteTimeTrigger(entity.Id.ToString(), TimeTriggerGroupId);
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
        return _timeTriggerService.GetTimeTrigger(entity.Id.ToString());
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


    private static void ShowOneShotTimerToastNotification(OneShotTimerEntity entity)
    {
        var args = new ToastArguments()
        {
            { TimersToastNotificationConstants.ArgumentKey_Action, TimersToastNotificationConstants.ArgumentValue_OneShot },
            { TimersToastNotificationConstants.ArgumentKey_Confirmed },
            { TimersToastNotificationConstants.ArgumentKey_TimerId, entity.Id.ToString() }
        };

        var tcb = new ToastContentBuilder();
        foreach (var arg in args)
        {
            tcb.AddArgument(arg.Key, arg.Value);
        }

        tcb.AddAudio(new Uri("ms-winsoundevent:Notification.Default", UriKind.RelativeOrAbsolute), silent: true)
            .AddText("OneShotTimer_ToastNotificationTitle".Translate())
            .AddAttributionText($"{entity.Title}\n{"Time_Elapsed".Translate(entity.Time.TranslateTimeSpan())}")
            .SetToastScenario(ToastScenario.Reminder)
            .AddButton("Close".Translate(), ToastActivationType.Background, args.ToString())
            .Show();
    }

    bool IToastActivationAware.ProcessToastActivation(ToastArguments args, ValueSet props)
    {
        if (!IToastActivationAware.IsContainAction(args, TimersToastNotificationConstants.ArgumentValue_OneShot)) { return false; }

        if (args.TryGetValue(TimersToastNotificationConstants.ArgumentKey_TimerId, out string timerId))
        {
            Guid entityId = Guid.Parse(timerId);
            if (_playCancelMap.Remove(entityId, out var cts))
            {
                cts.Cancel();
            }

            var entity = _oneShotTimerRepository.FindById(entityId);
            _messenger.Send(new OneShotTimerCheckedMessage(entity));            

            return true;
        }

        return false;
    }
}
