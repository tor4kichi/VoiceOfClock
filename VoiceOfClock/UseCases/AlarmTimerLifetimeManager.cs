using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
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
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using Windows.Foundation.Collections;

namespace VoiceOfClock.UseCases;

public sealed partial class AlarmTimerLifetimeManager : IApplicationLifeCycleAware, IToastActivationAware
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IMessenger _messenger;
    private readonly AlarmTimerRepository _alarmTimerRepository;

    private readonly ObservableCollection<AlarmTimerRunningInfo> _timers;
    public ReadOnlyObservableCollection<AlarmTimerRunningInfo> Timers { get; }

    public AlarmTimerLifetimeManager(
        IMessenger messenger
        , AlarmTimerRepository alarmTimerRepository
        )
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _messenger = messenger;
        _alarmTimerRepository = alarmTimerRepository;
        _timers = new ObservableCollection<AlarmTimerRunningInfo>(_alarmTimerRepository.ReadAllItems().Select(ToRunningInfo));
        Timers = new(_timers);
    }

    AlarmTimerRunningInfo ToRunningInfo(AlarmTimerEntity entity)
    {
        return new AlarmTimerRunningInfo(entity, _alarmTimerRepository, _dispatcherQueue, OnAlarmTrigger);
    }

    void OnAlarmTrigger(AlarmTimerRunningInfo runningInfo)
    {
        if (runningInfo.SoundSourceType == SoundSourceType.System)
        {
            _messenger.Send(new PlaySystemSoundRequest(Enum.Parse<WindowsNotificationSoundType>(runningInfo.SoundContent)));
        }
        else if (runningInfo.SoundSourceType == SoundSourceType.Tts)
        {
            _dispatcherQueue.TryEnqueue(async () => 
            {
                foreach (var i in Enumerable.Range(0, TimersToastNotificationConstants.VoiceNotificationRepeatCount))
                {
                    if (i != 0) 
                    {
                        await Task.Delay(TimersToastNotificationConstants.VoiceNotificationRepeatInterval); 
                    }

                    await _messenger.Send(new TextPlayVoiceRequest(runningInfo.SoundContent));
                }
            });            
        }
        else if (runningInfo.SoundSourceType == SoundSourceType.TtsWithSSML)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                foreach (var i in Enumerable.Range(0, TimersToastNotificationConstants.VoiceNotificationRepeatCount))
                {
                    if (i != 0)
                    {
                        await Task.Delay(TimersToastNotificationConstants.VoiceNotificationRepeatInterval);
                    }

                    await _messenger.Send(new SsmlPlayVoiceRequest(runningInfo.SoundContent));
                }
            });
        }

        var stopToastArgs = new ToastArguments()
        {
            { TimersToastNotificationConstants.ArgumentKey_Action, TimersToastNotificationConstants.ArgumentValue_Alarm },
            { TimersToastNotificationConstants.ArgumentKey_SnoozeStop },
            { TimersToastNotificationConstants.ArgumentKey_TimerId, runningInfo.Entity.Id.ToString() }
        };

        var againToastArgs = new ToastArguments()
        {
            { TimersToastNotificationConstants.ArgumentKey_Action, TimersToastNotificationConstants.ArgumentValue_Alarm },
            { TimersToastNotificationConstants.ArgumentKey_SnoozeAgain },
            { TimersToastNotificationConstants.ArgumentKey_TimerId, runningInfo.Entity.Id.ToString() }
        };

        if (TimeSpan.Zero < runningInfo.Snooze)
        {
            var tcb = new ToastContentBuilder();
            foreach (var kvp in againToastArgs)
            {
                tcb.AddArgument(kvp.Key, kvp.Value);
            }

            string defaultSelectComboBoxId = runningInfo.Snooze.Value.ToString();
            tcb.AddText("AlarmTimer_ToastNotificationTitle".Translate())                
                .AddAttributionText($"{runningInfo.Title}\n{runningInfo.TargetTime.ToShortTimeString()}\n\n{"AlarmTimer_ToastNotificationSnoozeTimeDescription".Translate()}")
                .AddComboBox(
                    TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id
                    , defaultSelectComboBoxId
                    , AlarmTimerConstants.SnoozeTimes.Select(x => (comboBoxItemKey: x.ToString(), comboBoxItemContent: "AlarmTimer_SnoozeWithMinutes".Translate(x.TotalMinutes))).ToArray()
                    )
                .AddButton("AlarmTimer_Snooze".Translate(), ToastActivationType.Background, againToastArgs.ToString())
                .AddButton("Close".Translate(), ToastActivationType.Background, stopToastArgs.ToString())
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
                .AddAttributionText($"{runningInfo.Title}\n{runningInfo.TargetTime.ToShortTimeString()}")
                .AddButton("Close".Translate(), ToastActivationType.Background, stopToastArgs.ToString())
                ;
            tcb.Show();
        }
    }


    public bool ProcessToastActivation(ToastArguments args, ValueSet props)
    {
        if (!IToastActivationAware.IsContainAction(args, TimersToastNotificationConstants.ArgumentValue_Alarm)) { return false; }

        string id = args.Get(TimersToastNotificationConstants.ArgumentKey_TimerId);
        Guid guid = new Guid(id);
        AlarmTimerRunningInfo? timer = Timers.FirstOrDefault(x => x.Entity.Id == guid);        
        if (timer is null) 
        {
            return false; 
        }
        else if (args.Contains(TimersToastNotificationConstants.ArgumentKey_SnoozeStop))
        {
            timer.AlarmChecked();

            return true;
        }
        else if (args.Contains(TimersToastNotificationConstants.ArgumentKey_SnoozeAgain))
        {
            TimeSpan snooze = TimeSpan.Parse((string)props[TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id]);
            timer.AlarmChecked(snooze);

            return true;
        }
        else
        {
            return false;
        }
    }

    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);
    }

    void IApplicationLifeCycleAware.Resuming()
    {
        
    }

    void IApplicationLifeCycleAware.Suspending()
    {
        
    }

    public AlarmTimerRunningInfo CreateAlarmTimer(string title, TimeOnly timeOfDay, DayOfWeek[] enabledDayOfWeeks, TimeSpan? snoozeTime, SoundSourceType soundSourceType, string soundContent)
    {
        AlarmTimerEntity newEntity = _alarmTimerRepository.CreateItem(new AlarmTimerEntity
        {
            Title = title,
            TimeOfDay = timeOfDay,
            EnabledDayOfWeeks = enabledDayOfWeeks,
            Snooze = snoozeTime,
            SoundSourceType = soundSourceType,
            SoundContent = soundContent,            
        });

        AlarmTimerRunningInfo newTimerRunningInfo = ToRunningInfo(newEntity);
        _timers.Add(newTimerRunningInfo);
        return newTimerRunningInfo;
    }

    public bool DeleteAlarmTimer(AlarmTimerRunningInfo runningInfo)
    {
        _timers.Remove(runningInfo);

        return _alarmTimerRepository.DeleteItem(runningInfo.Entity.Id);
    }
}
