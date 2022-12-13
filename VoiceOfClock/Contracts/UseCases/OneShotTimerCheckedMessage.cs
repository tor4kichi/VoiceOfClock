using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceOfClock.Core.Domain;

namespace VoiceOfClock.Contracts.UseCases;

public sealed class OneShotTimerCheckedMessage : ValueChangedMessage<OneShotTimerEntity>
{
    public OneShotTimerCheckedMessage(OneShotTimerEntity value) : base(value)
    {
    }
}
