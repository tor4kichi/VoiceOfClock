using System;

namespace VoiceOfClock.Core.Services;

public interface ITimeTriggerItem
{
    DateTime TriggerTime { get; }
    string Id { get; }
}
