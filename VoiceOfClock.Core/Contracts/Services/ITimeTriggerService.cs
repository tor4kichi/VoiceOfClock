using System.Numerics;
using VoiceOfClock.Core.Services;

namespace VoiceOfClock.Core.Contracts.Services;

public interface ITimeTriggerServiceBase<IdType> where IdType : notnull, IComparable<IdType>, IEquatable<IdType>
{
    void ClearTimeTriggerGroup(string groupId);
    void SetTimeTrigger(IdType id, DateTime triggerTime, string? groudId = null);
    void SetTimeTriggerGroup(string groudId, IEnumerable<(IdType id, DateTime triggerTime)> triggers);
    void DeleteTimeTrigger(IdType id, string? groudId = null);
    DateTime? GetTimeTrigger(IdType id);
    event EventHandler<TimeTriggeredEventArgs>? TimeTriggered;

    public sealed class TimeTriggeredEventArgs
    {
        public IdType Id { get; init; } = default(IdType);

        public DateTime TriggerTime { get; init; }

        public string? GroupId { get; init; }
    }
}

public interface ITimeTriggerService : ITimeTriggerServiceBase<Guid> { }

