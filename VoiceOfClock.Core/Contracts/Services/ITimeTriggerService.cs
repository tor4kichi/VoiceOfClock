using System.Numerics;
using VoiceOfClock.Core.Services;

namespace VoiceOfClock.Core.Contracts.Services;

public interface ITimeTriggerServiceBase<IdType> where IdType : notnull, IComparable<IdType>, IEquatable<IdType>
{
    void ClearTimeTriggerGroup(string groupId);
    ValueTask SetTimeTrigger(IdType id, DateTime triggerTime, string? groudId = null);
    ValueTask SetTimeTriggerGroup(string groudId, IEnumerable<(IdType id, DateTime triggerTime)> triggers);
    ValueTask DeleteTimeTrigger(IdType id, string? groudId = null);
    ValueTask<DateTime?> GetTimeTrigger(IdType id);
    event EventHandler<TimeTriggeredEventArgs>? TimeTriggered;

    public sealed class TimeTriggeredEventArgs
    {
        public IdType Id { get; init; } = default(IdType);

        public DateTime TriggerTime { get; init; }

        public string? GroupId { get; init; }
    }
}

public interface ITimeTriggerService : ITimeTriggerServiceBase<Guid> { }

