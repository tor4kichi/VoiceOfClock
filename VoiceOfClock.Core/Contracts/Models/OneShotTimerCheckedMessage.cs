using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Core.Contracts.Models;

public sealed class OneShotTimerCheckedMessage : ValueChangedMessage<OneShotTimerEntity>
{
    public OneShotTimerCheckedMessage(OneShotTimerEntity value) : base(value)
    {
    }
}
