using System;

namespace VoiceOfClock.Core.Contracts.Services;

public interface ISingleTimeTriggerBase<IdType> where IdType : IComparable<IdType>, IEquatable<IdType>
{
    void SetTimeTrigger(DateTime triggerTime, IdType argument);   
    event EventHandler<TimeTriggerRecievedEventArgs> TimeArrived;
    void Clear();

    public readonly record struct TimeTriggerRecievedEventArgs(DateTime TriggerTime, IdType argument);
}


public interface ISingleTimeTrigger : ISingleTimeTriggerBase<Guid> { }

