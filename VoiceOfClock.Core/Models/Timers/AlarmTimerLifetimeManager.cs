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
using System.Timers;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;
using static VoiceOfClock.Core.Contracts.Services.ITimeTriggerServiceBase<System.Guid>;

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

    public const string TimeTriggerGroupId = "Alarm";
    
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

        CancelTimerPlayingAudio(timer, NotifyAudioEndedReason.CancelledByUser);
        if (args.ContainsKey(TimersToastNotificationConstants.ArgumentKey_SnoozeStop))
        {
            TimerChecked(timer);
            e.IsHandled = true;
            return;
        }
        else if (args.ContainsKey(TimersToastNotificationConstants.ArgumentKey_SnoozeAgain))
        {
            TimeSpan snooze = TimeSpan.Parse((string)props[TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id]);
            _timeTriggerService.SetTimeTrigger(timer.Id, DateTime.Now + snooze, TimeTriggerGroupId);
            e.IsHandled = true;
            return;
        }
    }

    private void OnTimeTriggered(object? sender, TimeTriggeredEventArgs e)
    {
        if (e.GroupId != TimeTriggerGroupId) { return; }
        
        AlarmTimerEntity? timer = _alarmTimerRepository.FindById(e.Id);        
        if (timer == null) { return; }

        if (timer.TimeZoneId != null
            && TimeZoneInfo.Local.Id != timer.TimeZoneId)
        {
            var targetTZTime = TimeZoneInfo.ConvertTime(e.TriggerTime, TimeZoneInfo.Local, TimeZoneInfo.FindSystemTimeZoneById(timer.TimeZoneId));
            _toastNotificationService.ShowAlarmToastNotification(timer, targetTZTime);
        }
        else
        {
            _toastNotificationService.ShowAlarmToastNotification(timer, e.TriggerTime);
        }

        PlayTimerSound(timer);
    }



    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);
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

    private void CancelTimerPlayingAudio(AlarmTimerEntity entity, NotifyAudioEndedReason endedReason)
    {
        if (_playCancelMap.Remove(entity.Id, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();

            _messenger.Send(new NotifyAudioEndedMessage(entity, endedReason));
        }
    }

    private readonly Dictionary<Guid, CancellationTokenSource> _playCancelMap = new();

    private async void PlayTimerSound(AlarmTimerEntity entity)
    {
        CancelTimerPlayingAudio(entity, NotifyAudioEndedReason.CancelledFromNextNotify);
        CancellationTokenSource cts = new CancellationTokenSource();
        _playCancelMap.Add(entity.Id, cts);
        CancellationToken ct = cts.Token;
        try
        {
            _messenger.Send(new NotifyAudioStartingMessage(entity));
            await _soundContentPlayerService.PlaySoundContentAsync(entity.SoundSourceType, entity.SoundContent, cancellationToken: ct);
        }
        catch (OperationCanceledException) { }
        catch
        {
            CancelTimerPlayingAudio(entity, NotifyAudioEndedReason.Unknown);
        }
    }

    public bool GetNowPlayingAudio(AlarmTimerEntity entity)
    {
        return _playCancelMap.ContainsKey(entity.Id);
    }

    private DateTime CulcNextTime(AlarmTimerEntity timer)
    {
        if (timer.TimeZoneId != null
            && TimeZoneInfo.Local.Id != timer.TimeZoneId
            )
        {
            return TimeHelpers.CulcNextTimeWithTimeZone(
                DateTimeOffset.Now, 
                timer.TimeOfDay.ToTimeSpan(), 
                timer.EnabledDayOfWeeks, 
                TimeZoneInfo.Local, 
                TimeZoneInfo.FindSystemTimeZoneById(timer.TimeZoneId)
                );
        }
        else
        {
            return TimeHelpers.CulcNextTime(DateTime.Now, timer.TimeOfDay.ToTimeSpan(), timer.EnabledDayOfWeeks);
        }
    }

    public void TimerChecked(AlarmTimerEntity timer)
    {
        if (timer.IsEnabled && timer.EnabledDayOfWeeks.Any())
        {
            var nextAlarmTime = CulcNextTime(timer);
            _timeTriggerService.SetTimeTrigger(timer.Id, nextAlarmTime, TimeTriggerGroupId);
        }

        CancelTimerPlayingAudio(timer, NotifyAudioEndedReason.CancelledByUser);
        _toastNotificationService.HideNotify(timer);
    }

    public DateTime SetSnooze(AlarmTimerEntity timer)
    {
        CancelTimerPlayingAudio(timer, NotifyAudioEndedReason.CancelledByUser);
        _toastNotificationService.HideNotify(timer);
        var nextAlarmTime = DateTime.Now + timer.Snooze ?? throw new InvalidOperationException();
        _timeTriggerService.SetTimeTrigger(timer.Id, nextAlarmTime, TimeTriggerGroupId);
        return nextAlarmTime;
    }    
   
    public List<AlarmTimerEntity> GetAlarmTimers()
    {
        return _alarmTimerRepository.ReadAllItems();
    }

    public AlarmTimerEntity CreateAlarmTimer(string title, TimeOnly timeOfDay, DayOfWeek[] enabledDayOfWeeks, TimeSpan? snoozeTime, SoundSourceType soundSourceType, string soundContent, bool isEnabled = true, string? timeZoneId = null)
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
            TimeZoneId = timeZoneId
        });

        // 並び順を確実に指定する
        foreach (var (timer, index) in _alarmTimerRepository.ReadAllItems().OrderBy(x => x.Order).Select((x, i) => (x, i)))
        {
            timer.Order = index;
            _alarmTimerRepository.UpdateItem(timer);
        }

        if (newEntity.IsEnabled && newEntity.EnabledDayOfWeeks.Any())
        {
            _timeTriggerService.SetTimeTrigger(newEntity.Id, CulcNextTime(newEntity), TimeTriggerGroupId);
        }

        return newEntity;
    }

    public bool DeleteAlarmTimer(AlarmTimerEntity entity)
    {
        var result = _alarmTimerRepository.DeleteItem(entity.Id);

        _timeTriggerService.DeleteTimeTrigger(entity.Id, TimeTriggerGroupId);

        return result;
    }

    public void UpdateAlarmTimer(AlarmTimerEntity entity)
    {
        _alarmTimerRepository.UpdateItem(entity);
        _messenger.Send(new AlarmTimerUpdatedMessage(entity));
       
        if (entity.IsEnabled)
        {
            _timeTriggerService.SetTimeTrigger(entity.Id, CulcNextTime(entity), TimeTriggerGroupId);
        }
        else
        {
            _timeTriggerService.DeleteTimeTrigger(entity.Id);
        }        
    }

}
