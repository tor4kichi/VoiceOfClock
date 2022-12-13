using System;

namespace VoiceOfClock.Core.Contracts.Services;

public interface ISingleTimeTrigger
{
    void SetTimeTrigger(DateTime triggerTime, string argument);   
    event EventHandler<TimeTriggerRecievedEventArgs> TimeArrived;
    void Clear();
}

public readonly record struct TimeTriggerRecievedEventArgs(DateTime TriggerTime, string argument);
