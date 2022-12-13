using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Domain;

namespace VoiceOfClock.Contracts.UseCases;

public sealed class AlarmTimerValueChangedMessage : ValueChangedMessage<AlarmTimerEntity>
{
    public AlarmTimerValueChangedMessage(AlarmTimerEntity value) : base(value)
    {
    }
}
