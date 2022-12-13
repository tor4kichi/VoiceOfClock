using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;

namespace VoiceOfClock.Core.Models.Timers;

public sealed partial class AlarmTimerLifetimeManager     
    : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
    , IRecipient<ToastNotificationActivatedMessage>
{
    private readonly IMessenger _messenger;
    private readonly ILocalizationService _localizationService;
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly ITimeTriggerService _timeTriggerService;
    private readonly IToastNotificationService _toastNotificationService;
    private readonly AlarmTimerRepository _alarmTimerRepository;

    private const string TimeTriggerGroupId = nameof(AlarmTimerLifetimeManager);
    
    public AlarmTimerLifetimeManager(    
        IMessenger messenger
        , ILocalizationService localizationService
        , ISoundContentPlayerService soundContentPlayerService       
        , ITimeTriggerService timeTriggerService
        , IToastNotificationService toastNotificationService
        , AlarmTimerRepository alarmTimerRepository
        )
    {
        _messenger = messenger;
        _localizationService = localizationService;
        _soundContentPlayerService = soundContentPlayerService;
        _timeTriggerService = timeTriggerService;
        _toastNotificationService = toastNotificationService;
        _alarmTimerRepository = alarmTimerRepository;

        _timeTriggerService.TimeTriggered += OnTimeTriggered;
    }

    void IRecipient<ToastNotificationActivatedMessage>.Receive(ToastNotificationActivatedMessage message)
    {
        var e = message.Value;
        if (e.IsHandled) { return; }

        var args = e.Args;
        var props = e.Props;
        if (!IToastNotificationService.IsContainAction(args, TimersToastNotificationConstants.ArgumentValue_Alarm))
        {
            return;
        }
        string timerId = args[TimersToastNotificationConstants.ArgumentKey_TimerId];
        Guid entityId = Guid.Parse(timerId);
        AlarmTimerEntity? timer = _alarmTimerRepository.FindById(entityId);
        if (timer is null)
        {
            return;
        }
        else if (args.ContainsKey(TimersToastNotificationConstants.ArgumentKey_SnoozeStop))
        {
            TimerChecked(timer);
            e.IsHandled = true;
            return;
        }
        else if (args.ContainsKey(TimersToastNotificationConstants.ArgumentKey_SnoozeAgain))
        {
            TimeSpan snooze = TimeSpan.Parse((string)props[TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id]);
            var nextAlarmTime = DateTime.Now + snooze;
            _timeTriggerService.SetTimeTrigger(timerId, nextAlarmTime, TimeTriggerGroupId);

            if (_playCancelMap.Remove(entityId, out var cts))
            {
                cts.Cancel();
            }
            e.IsHandled = true;
            return;
        }
    }

    private void OnTimeTriggered(object? sender, TimeTriggeredEventArgs e)
    {
        if (e.GroupId != TimeTriggerGroupId) { return; }

        Guid entityId = Guid.Parse(e.Id);
        AlarmTimerEntity? timer = _alarmTimerRepository.FindById(entityId);        
        if (timer == null) { return; }

        _toastNotificationService.ShowAlarmToastNotification(timer, e.TriggerTime);
        PlayTimerSound(timer);
    }



    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);

        _timeTriggerService.ClearTimeTrigger(TimeTriggerGroupId);
        DateTime now = DateTime.Now;
        _timeTriggerService.SetTimeTriggerGroup(TimeTriggerGroupId,
            _alarmTimerRepository.ReadAllItems()
            .Where(x => x.IsEnabled && x.EnabledDayOfWeeks.Any())
            .Select(x => (x.Id.ToString(), TimeHelpers.CulcNextTime(now, x.TimeOfDay.ToTimeSpan(), x.EnabledDayOfWeeks)))
            );
    }

    void IApplicationLifeCycleAware.Resuming() { }

    void IApplicationLifeCycleAware.Suspending() { }

    
    void IRecipient<ActiveTimerCollectionRequestMessage>.Receive(ActiveTimerCollectionRequestMessage message)
    {
        foreach (var timer in GetAlarmTimers())
        {
            if (timer.IsEnabled)
            {
                message.Reply(timer);
            }
        }
    }

    private readonly Dictionary<Guid, CancellationTokenSource> _playCancelMap = new();

    private async void PlayTimerSound(AlarmTimerEntity entity)
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

    


    public void TimerChecked(AlarmTimerEntity timer)
    {
        if (timer.IsEnabled && timer.EnabledDayOfWeeks.Any())
        {
            var nextAlarmTime = TimeHelpers.CulcNextTime(DateTime.Now, timer.TimeOfDay.ToTimeSpan(), timer.EnabledDayOfWeeks);
            _timeTriggerService.SetTimeTrigger(timer.Id.ToString(), nextAlarmTime, TimeTriggerGroupId);
        }

        if (_playCancelMap.Remove(timer.Id, out var cts))
        {
            cts.Cancel();
        }
    }

    public List<AlarmTimerEntity> GetAlarmTimers()
    {
        return _alarmTimerRepository.ReadAllItems();
    }

    public AlarmTimerEntity CreateAlarmTimer(string title, TimeOnly timeOfDay, DayOfWeek[] enabledDayOfWeeks, TimeSpan? snoozeTime, SoundSourceType soundSourceType, string soundContent, bool isEnabled = true)
    {
        AlarmTimerEntity newEntity = _alarmTimerRepository.CreateItem(new AlarmTimerEntity
        {
            Title = title,
            TimeOfDay = timeOfDay,
            EnabledDayOfWeeks = enabledDayOfWeeks,
            Snooze = snoozeTime,
            SoundSourceType = soundSourceType,
            SoundContent = soundContent,            
            IsEnabled = isEnabled,
            Order = int.MaxValue,
        });

        // 並び順を確実に指定する
        foreach (var (timer, index) in _alarmTimerRepository.ReadAllItems().OrderBy(x => x.Order).Select((x, i) => (x, i)))
        {
            timer.Order = index;
            _alarmTimerRepository.UpdateItem(timer);
        }

        if (newEntity.IsEnabled && newEntity.EnabledDayOfWeeks.Any())
        {
            _timeTriggerService.SetTimeTrigger(newEntity.Id.ToString(), TimeHelpers.CulcNextTime(DateTime.Now, newEntity.TimeOfDay.ToTimeSpan(), newEntity.EnabledDayOfWeeks), TimeTriggerGroupId);
        }

        return newEntity;
    }

    public bool DeleteAlarmTimer(AlarmTimerEntity entity)
    {
        var result = _alarmTimerRepository.DeleteItem(entity.Id);

        _timeTriggerService.DeleteTimeTrigger(entity.Id.ToString(), TimeTriggerGroupId);

        return result;
    }

    public void UpdateAlarmTimer(AlarmTimerEntity entity)
    {
        _alarmTimerRepository.UpdateItem(entity);
        _messenger.Send(new AlarmTimerUpdatedMessage(entity));
       
        if (entity.IsEnabled && entity.EnabledDayOfWeeks.Any())
        {
            _timeTriggerService.SetTimeTrigger(entity.Id.ToString(), TimeHelpers.CulcNextTime(DateTime.Now, entity.TimeOfDay.ToTimeSpan(), entity.EnabledDayOfWeeks), TimeTriggerGroupId);
        }
        else
        {
            _timeTriggerService.DeleteTimeTrigger(entity.Id.ToString());
        }        
    }

}
