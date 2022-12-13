using VoiceOfClock.Core.Services;

namespace VoiceOfClock.Core.Contracts.Services;

public interface ITimeTriggerService
{
    void ClearTimeTrigger(string id);
    ValueTask SetTimeTrigger(string id, DateTime triggerTime, string? groud_id = null);
    ValueTask SetTimeTriggerGroup(string? groud_id, IEnumerable<(string id, DateTime triggerTime)> triggers);
    ValueTask DeleteTimeTrigger(string id, string? groud_id = null);
    ValueTask<DateTime?> GetTimeTrigger(string id);
    event EventHandler<TimeTriggeredEventArgs>? TimeTriggered;


}


public sealed class TimeTriggeredEventArgs
{
    public string Id { get; init; }

    public DateTime TriggerTime { get; init; }

    public string? GroupId { get; init; }
}