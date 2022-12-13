using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Domain;

namespace VoiceOfClock.Contracts.UseCases;

public sealed class ActiveTimerCollectionRequestMessage : CollectionRequestMessage<ITimer>
{
}
