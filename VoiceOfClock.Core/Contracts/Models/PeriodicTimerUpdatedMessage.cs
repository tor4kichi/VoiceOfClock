using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Core.Contracts.Models;

public sealed class PeriodicTimerUpdatedMessage : ValueChangedMessage<PeriodicTimerEntity>
{
    public PeriodicTimerUpdatedMessage(PeriodicTimerEntity value) : base(value)
    {
    }
}

public sealed class PeriodicTimerProgressPeriodMessage : ValueChangedMessage<PeriodicTimerEntity>
{
    public PeriodicTimerProgressPeriodMessage(PeriodicTimerEntity value) : base(value)
    {
    }
}