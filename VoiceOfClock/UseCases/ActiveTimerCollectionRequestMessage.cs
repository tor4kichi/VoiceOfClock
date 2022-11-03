using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.UseCases;

public sealed class ActiveTimerCollectionRequestMessage : CollectionRequestMessage<IRunningTimer>
{
}
