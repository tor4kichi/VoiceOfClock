using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models.Timers;
using VoiceOfClock.ViewModels;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;

namespace VoiceOfClock.Services;

// ドメイン知識を持たせない
// トースト通知のワークのみに専念する
public sealed class ToastNotificationService : Core.Contracts.Services.IToastNotificationService, Contracts.Services.IToastActivationAware
{
    private readonly IMessenger _messenger;

    public ToastNotificationService(IMessenger messenger)
    {
        _messenger = messenger;
    }

    private readonly ConcurrentDictionary<Guid, ToastNotification> _notificationsMap = new();
    void Core.Contracts.Services.IToastNotificationService.ShowAlarmToastNotification(AlarmTimerEntity entity, DateTime targetTime)
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

        string timeText = entity.TimeZoneId != TimeZoneInfo.Local.Id
            ? $"{targetTime.ToShortTimeString()} {TimeZoneInfo.FindSystemTimeZoneById(entity.TimeZoneId).StandardName}"
            : targetTime.ToShortTimeString()
            ;

        var tcb = new ToastContentBuilder();
        if (TimeSpan.Zero < entity.Snooze)
        {            
            foreach (var kvp in againToastArgs)
            {
                tcb.AddArgument(kvp.Key, kvp.Value);
            }

            string defaultSelectComboBoxId = entity.Snooze.Value.ToString();
            tcb.AddText("AlarmTimer_ToastNotificationTitle".Translate())
                .AddAttributionText($"{entity.Title}\n{timeText}\n\n{"AlarmTimer_ToastNotificationSnoozeTimeDescription".Translate()}")
                .AddComboBox(
                    TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id
                    , defaultSelectComboBoxId
                    , AlarmTimerConstants.SnoozeTimes.Select(x => (comboBoxItemKey: x.ToString(), comboBoxItemContent: "AlarmTimer_SnoozeWithMinutes".Translate(x.TotalMinutes))).ToArray()
                    )
                .AddButton("AlarmTimer_Snooze".Translate(), ToastActivationType.Background, againToastArgs.ToString())
                .AddButton("Close".Translate(), ToastActivationType.Background, stopToastArgs.ToString())
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default", UriKind.RelativeOrAbsolute), silent: true)
                .SetToastScenario(ToastScenario.Alarm)
                ;
        }
        else
        {
            foreach (var kvp in stopToastArgs)
            {
                tcb.AddArgument(kvp.Key, kvp.Value);
            }

            tcb.AddText("AlarmTimer_ToastNotificationTitle".Translate())
                .AddAttributionText($"{entity.Title}\n{timeText}")
                .AddButton("Close".Translate(), ToastActivationType.Background, stopToastArgs.ToString())
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default", UriKind.RelativeOrAbsolute), silent: true)
                .SetToastScenario(ToastScenario.Alarm)
                ;
        }

        ToastContentBuilderShow(entity, tcb);
    }

    void Core.Contracts.Services.IToastNotificationService.ShowOneShotTimerToastNotification(OneShotTimerEntity entity)
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
            ;

        ToastContentBuilderShow(entity, tcb);
    }


    void ToastContentBuilderShow(ITimer timer, ToastContentBuilder tcb)
    {
        tcb.Show(
            (toast) =>
            {
                _notificationsMap.TryAdd(timer.Id, toast);
                toast.Dismissed += (s, e) => { _notificationsMap.TryRemove(timer.Id, out _); };
                toast.Failed += (s, e) => { _notificationsMap.TryRemove(timer.Id, out _); };
                toast.Activated += (s, e) => { _notificationsMap.TryRemove(timer.Id, out _); };
            }
            );
    }


    bool Contracts.Services.IToastActivationAware.ProcessToastActivation(ToastArguments args, ValueSet props)
    {
        var e = new ToastNotificationActivatedEventArgs(new Dictionary<string, string>(args), props);
        _messenger.Send(new ToastNotificationActivatedMessage(e));
        return e.IsHandled;        
    }

    public void HideNotify(ITimer timer)
    {
        if (_notificationsMap.TryRemove(timer.Id, out ToastNotification toast))
        {
            ToastNotificationManagerCompat.CreateToastNotifier().Hide(toast);
        }
    }
}

