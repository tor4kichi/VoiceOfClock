using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceOfClock.Core.Domain;

namespace VoiceOfClock.Contracts.UseCases;

public sealed class UpdatePeriodicTimerMessage : ValueChangedMessage<PeriodicTimerEntity>
{
    public UpdatePeriodicTimerMessage(PeriodicTimerEntity value) : base(value)
    {
    }
}

public sealed class ProgressPeriodPeriodicTimerMessage : ValueChangedMessage<PeriodicTimerEntity>
{
    public ProgressPeriodPeriodicTimerMessage(PeriodicTimerEntity value) : base(value)
    {
    }
}