using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Core.Contracts.Models;

public sealed class AlarmTimerUpdatedMessage : ValueChangedMessage<AlarmTimerEntity>
{
    public AlarmTimerUpdatedMessage(AlarmTimerEntity value) : base(value)
    {
    }
}
