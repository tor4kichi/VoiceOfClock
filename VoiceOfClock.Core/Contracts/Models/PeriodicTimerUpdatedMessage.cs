using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Core.Contracts.Models;

public sealed class PeriodicTimerUpdatedMessage : ValueChangedMessage<PeriodicTimerEntity>
{
    public PeriodicTimerUpdatedMessage(PeriodicTimerEntity value, DateTime triggerTime) : base(value)
    {
        TriggerTime = triggerTime;
    }

    public DateTime TriggerTime { get; }
}

public sealed class PeriodicTimerProgressPeriodMessage : ValueChangedMessage<PeriodicTimerEntity>
{
    public PeriodicTimerProgressPeriodMessage(PeriodicTimerEntity value, DateTime triggerTime) : base(value)
    {
        TriggerTime = triggerTime;
    }

    public DateTime TriggerTime { get; }
}