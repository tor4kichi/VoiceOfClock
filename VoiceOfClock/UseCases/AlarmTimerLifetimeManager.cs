using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using I18NPortable;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Contracts.UseCases;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.Services.SoundPlayer;
using Windows.Foundation.Collections;

namespace VoiceOfClock.UseCases;

public sealed partial class AlarmTimerLifetimeManager     
    : IApplicationLifeCycleAware
    , IToastActivationAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
{
    private readonly IMessenger _messenger;
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly ITimeTriggerService _timeTriggerService;
    private readonly AlarmTimerRepository _alarmTimerRepository;

    private const string AlarmTimerTriggerGroupId = nameof(AlarmTimerLifetimeManager);
    
    public AlarmTimerLifetimeManager(    
        IMessenger messenger
        , ISoundContentPlayerService soundContentPlayerService       
        , ITimeTriggerService timeTriggerService
        , AlarmTimerRepository alarmTimerRepository
        )
    {
        _messenger = messenger;
        _soundContentPlayerService = soundContentPlayerService;
        _timeTriggerService = timeTriggerService;
        _alarmTimerRepository = alarmTimerRepository;

        _timeTriggerService.TimeTriggered += OnTimeTriggered;
    }

    private void OnTimeTriggered(object? sender, TimeTriggeredEventArgs e)
    {
        if (e.GroupId != AlarmTimerTriggerGroupId) { return; }

        Guid entityId = Guid.Parse(e.Id);
        AlarmTimerEntity? timer = _alarmTimerRepository.FindById(entityId);        
        if (timer == null) { return; }

        ShowAlarmToastNotification(timer, e.TriggerTime);
        PlayTimerSound(timer);
    }

    private readonly Dictionary<Guid, CancellationTokenSource> _playCancelMap = new();

    private async void PlayTimerSound(AlarmTimerEntity entity)
    {
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

    private static void ShowAlarmToastNotification(AlarmTimerEntity entity, DateTime targetTime)
   {
        var stopToastArgs = new ToastArguments()
        {
            { TimersToastNotificationConstants.ArgumentKey_Action, TimersToastNotificationConstants.ArgumentValue_Alarm },
            { TimersToastNotificationConstants.ArgumentKey_SnoozeStop },
            { TimersToastNotificationConstants.ArgumentKey_TimerId, entity.Id.ToString() }
        };

        var againToastArgs = new ToastArguments()
        {
            { TimersToastNotificationConstants.ArgumentKey_Action, TimersToastNotificationConstants.ArgumentValue_Alarm },
            { TimersToastNotificationConstants.ArgumentKey_SnoozeAgain },
            { TimersToastNotificationConstants.ArgumentKey_TimerId, entity.Id.ToString() }
        };

        if (TimeSpan.Zero < entity.Snooze)
        {
            var tcb = new ToastContentBuilder();
            foreach (var kvp in againToastArgs)
            {
                tcb.AddArgument(kvp.Key, kvp.Value);
            }

            string defaultSelectComboBoxId = entity.Snooze.Value.ToString();
            tcb.AddText("AlarmTimer_ToastNotificationTitle".Translate())
                .AddAttributionText($"{entity.Title}\n{targetTime.ToShortTimeString()}\n\n{"AlarmTimer_ToastNotificationSnoozeTimeDescription".Translate()}")
                .AddComboBox(
                    TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id
                    , defaultSelectComboBoxId
                    , AlarmTimerConstants.SnoozeTimes.Select(x => (comboBoxItemKey: x.ToString(), comboBoxItemContent: "AlarmTimer_SnoozeWithMinutes".Translate(x.TotalMinutes))).ToArray()
                    )
                .AddButton("AlarmTimer_Snooze".Translate(), ToastActivationType.Background, againToastArgs.ToString())
                .AddButton("Close".Translate(), ToastActivationType.Background, stopToastArgs.ToString())
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default", UriKind.RelativeOrAbsolute), silent: true)
                ;
            tcb.Show();
        }
        else
        {
            var tcb = new ToastContentBuilder();
            foreach (var kvp in stopToastArgs)
            {
                tcb.AddArgument(kvp.Key, kvp.Value);
            }

            tcb.AddText("AlarmTimer_ToastNotificationTitle".Translate())
                .AddAttributionText($"{entity.Title}\n{targetTime.ToShortTimeString()}")
                .AddButton("Close".Translate(), ToastActivationType.Background, stopToastArgs.ToString())
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default", UriKind.RelativeOrAbsolute), silent: true)
                ;
            tcb.Show();
        }
    }

    public bool ProcessToastActivation(ToastArguments args, ValueSet props)
    {
        if (!IToastActivationAware.IsContainAction(args, TimersToastNotificationConstants.ArgumentValue_Alarm)) { return false; }

        string timerId = args.Get(TimersToastNotificationConstants.ArgumentKey_TimerId);
        Guid entityId = Guid.Parse(timerId);
        AlarmTimerEntity? timer = _alarmTimerRepository.FindById(entityId);
        if (timer is null) 
        {
            return false; 
        }
        else if (args.Contains(TimersToastNotificationConstants.ArgumentKey_SnoozeStop))
        {
            TimerChecked(timer);
            return true;
        }
        else if (args.Contains(TimersToastNotificationConstants.ArgumentKey_SnoozeAgain))
        {            
            TimeSpan snooze = TimeSpan.Parse((string)props[TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id]);
            var nextAlarmTime = DateTime.Now + snooze;
            _timeTriggerService.SetTimeTrigger(timerId, nextAlarmTime, AlarmTimerTriggerGroupId);

            if (_playCancelMap.Remove(entityId, out var cts))
            {
                cts.Cancel();
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    public void TimerChecked(AlarmTimerEntity timer)
    {
        if (timer.IsEnabled && timer.EnabledDayOfWeeks.Any())
        {
            var nextAlarmTime = TimeHelpers.CulcNextTime(DateTime.Now, timer.TimeOfDay.ToTimeSpan(), timer.EnabledDayOfWeeks);
            _timeTriggerService.SetTimeTrigger(timer.Id.ToString(), nextAlarmTime, AlarmTimerTriggerGroupId);
        }

        if (_playCancelMap.Remove(timer.Id, out var cts))
        {
            cts.Cancel();
        }
    }

    void IApplicationLifeCycleAware.Initialize()
    {
        if (SystemInformation.Instance.IsFirstRun)
        {
            CreateAlarmTimer("AlarmTimer_TemporaryTitle".Translate(1), TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), Enum.GetValues<DayOfWeek>(), null, SoundSourceType.System, WindowsNotificationSoundType.Reminder.ToString(), isEnabled: false);
        }

        _timeTriggerService.ClearTimeTrigger(AlarmTimerTriggerGroupId);
        DateTime now = DateTime.Now;
        _timeTriggerService.SetTimeTriggerGroup(AlarmTimerTriggerGroupId,
            _alarmTimerRepository.ReadAllItems()
            .Where(x => x.IsEnabled && x.EnabledDayOfWeeks.Any())
            .Select(x => (x.Id.ToString(), TimeHelpers.CulcNextTime(now, x.TimeOfDay.ToTimeSpan(), x.EnabledDayOfWeeks)))
            );
    }

    void IApplicationLifeCycleAware.Resuming()
    {
        
    }

    void IApplicationLifeCycleAware.Suspending()
    {
        
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
            _timeTriggerService.SetTimeTrigger(newEntity.Id.ToString(), TimeHelpers.CulcNextTime(DateTime.Now, newEntity.TimeOfDay.ToTimeSpan(), newEntity.EnabledDayOfWeeks), AlarmTimerTriggerGroupId);
        }

        return newEntity;
    }

    public bool DeleteAlarmTimer(AlarmTimerEntity entity)
    {
        var result = _alarmTimerRepository.DeleteItem(entity.Id);

        _timeTriggerService.DeleteTimeTrigger(entity.Id.ToString(), AlarmTimerTriggerGroupId);

        return result;
    }

    public void UpdateAlarmTimer(AlarmTimerEntity entity)
    {
        _alarmTimerRepository.UpdateItem(entity);
        _messenger.Send(new AlarmTimerValueChangedMessage(entity));
       
        if (entity.IsEnabled && entity.EnabledDayOfWeeks.Any())
        {
            _timeTriggerService.SetTimeTrigger(entity.Id.ToString(), TimeHelpers.CulcNextTime(DateTime.Now, entity.TimeOfDay.ToTimeSpan(), entity.EnabledDayOfWeeks), AlarmTimerTriggerGroupId);
        }
        else
        {
            _timeTriggerService.DeleteTimeTrigger(entity.Id.ToString());
        }        
    }



    public List<AlarmTimerEntity> GetAlarmTimers()
    {
        return _alarmTimerRepository.ReadAllItems();
    }

    void IRecipient<ActiveTimerCollectionRequestMessage>.Receive(ActiveTimerCollectionRequestMessage message)
    {
        foreach (var timer in GetAlarmTimers())
        {
            message.Reply(timer);
        }
    }
}
