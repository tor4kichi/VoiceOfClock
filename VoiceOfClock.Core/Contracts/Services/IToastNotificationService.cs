using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Core.Contracts.Services;

public interface IToastNotificationService
{
    void ShowAlarmToastNotification(AlarmTimerEntity entity, DateTime targetTime);
    void ShowOneShotTimerToastNotification(OneShotTimerEntity entity);

    static bool IsContainAction(IDictionary<string, string> args, string targetAction)
    {
        return args.TryGetValue(TimersToastNotificationConstants.ArgumentKey_Action, out string? action)
            && action == targetAction
            ;
    }
}

public sealed class ToastNotificationActivatedEventArgs
{
    public ToastNotificationActivatedEventArgs(IDictionary<string, string> args, IDictionary<string, object> props)
    {
        Args = args;
        Props = props;
    }

    public IDictionary<string, string> Args { get; }
    public IDictionary<string, object> Props { get; }

    public bool IsHandled { get; set; }
}

public sealed class ToastNotificationActivatedMessage : ValueChangedMessage<ToastNotificationActivatedEventArgs>
{
    public ToastNotificationActivatedMessage(ToastNotificationActivatedEventArgs value) : base(value)
    {
    }
}
