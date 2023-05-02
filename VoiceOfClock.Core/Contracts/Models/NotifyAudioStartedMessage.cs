using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Core.Contracts.Models;
public sealed class NotifyAudioStartingMessage : ValueChangedMessage<ITimer>
{
    public NotifyAudioStartingMessage(ITimer value) : base(value)
    {
    }
}

public sealed class NotifyAudioEndedMessage : ValueChangedMessage<ITimer>
{
    public NotifyAudioEndedMessage(ITimer value, NotifyAudioEndedReason endedReason) : base(value)
    {
        EndedReason = endedReason;
    }

    public NotifyAudioEndedReason EndedReason { get; }
}

public enum NotifyAudioEndedReason
{
    Unknown,
    Completed,    
    CancelledByUser,
    CancelledFromNextNotify,
}