using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;

namespace VoiceOfClock.Core.Services;


public sealed class TimeTriggerEntity
{
    [BsonId]
    public string Id { get; set; }

    public DateTime TriggerTime { get; set; }

    public string? GroupId { get; set; }
}


public sealed class TimeTriggerRepository : Core.Infrastructure.LiteDBRepositoryBase<TimeTriggerEntity>
{
    public TimeTriggerRepository(ILiteDatabase database)
         : base(database)
    {
        _collection.EnsureIndex(x => x.TriggerTime);
    }

    public TimeTriggerEntity? GetNextTrigger()
    {
        return _collection.Query().OrderBy(x => x.TriggerTime).FirstOrDefault();
    }
}


public sealed class TimeTriggerService : IDisposable, ITimeTriggerService
{
    private readonly ISingleTimeTrigger _timeTrigger;
    private readonly TimeTriggerRepository _timeTriggerRepository;

    public event EventHandler<TimeTriggeredEventArgs>? TimeTriggered;


    public TimeTriggerService(
        ISingleTimeTrigger timeTrigger
        , TimeTriggerRepository timeTriggerRepository
        )
    {
        _timeTrigger = timeTrigger;
        _timeTriggerRepository = timeTriggerRepository;
        _timeTrigger.TimeArrived += OnTimeArrived;
    }

    public void Dispose()
    {
        _timeTrigger.TimeArrived -= OnTimeArrived;
    }

    private void OnTimeArrived(object? sender, TimeTriggerRecievedEventArgs e)
    {
        var entity = _timeTriggerRepository.FindById(e.argument);
        if (entity == null)
        {
            throw new InvalidOperationException();
        }

        _timeTriggerRepository.DeleteItem(entity.Id);

        UpdateNextTrigger();

        if (DateTime.Now - e.TriggerTime < TimeSpan.FromSeconds(3))
        {
            TimeTriggered?.Invoke(this, new TimeTriggeredEventArgs()
            {
                Id = entity.Id,
                TriggerTime = entity.TriggerTime,
                GroupId = entity.GroupId,
            });
        }        
    }

    public ValueTask SetTimeTrigger(string id, DateTime triggerTime, string? groud_id = null)
    {
        if (_timeTriggerRepository.Exists(x => x.Id == id))
        {
            _timeTriggerRepository.DeleteItem(id);
        }

        _timeTriggerRepository.CreateItem(new TimeTriggerEntity { Id = id, TriggerTime = triggerTime, GroupId = groud_id });

        UpdateNextTrigger();

        return new ValueTask();
    }

    public ValueTask SetTimeTriggerGroup(string? groud_id, IEnumerable<(string id, DateTime triggerTime)> triggers)
    {
        foreach (var trigger in triggers)
        {
            if (_timeTriggerRepository.Exists(x => x.Id == trigger.id))
            {
                _timeTriggerRepository.DeleteItem(trigger.id);
            }

            _timeTriggerRepository.CreateItem(new TimeTriggerEntity { Id = trigger.id, TriggerTime = trigger.triggerTime, GroupId = groud_id });
        }

        UpdateNextTrigger();

        return new ValueTask();
    }

    private void UpdateNextTrigger()
    {
        var nextTrigger = _timeTriggerRepository.GetNextTrigger();
        if (nextTrigger != null)
        {
            _timeTrigger.SetTimeTrigger(nextTrigger.TriggerTime, nextTrigger.Id);
        }
        else
        {
            _timeTrigger.Clear();
        }
    }

    public void ClearTimeTriggerGroup(string groupId)
    {
        _timeTriggerRepository.DeleteMany(x => x.GroupId == groupId);

        UpdateNextTrigger();
    }

    public void ClearTimeTrigger(string id)
    {
        if (_timeTriggerRepository.Exists(x => x.Id == id))
        {
            _timeTriggerRepository.DeleteItem(id);
        }

        UpdateNextTrigger();
    }

    public ValueTask DeleteTimeTrigger(string id, string? groud_id = null)
    {
        if (groud_id == null)
        {
            _timeTriggerRepository.DeleteItem(id);
        }
        else
        {
            _timeTriggerRepository.DeleteMany(x => x.GroupId == groud_id && x.Id == id);
        }

        UpdateNextTrigger();

        return new();
    }

    public ValueTask<DateTime?> GetTimeTrigger(string id)
    {
        var timer = _timeTriggerRepository.FindById(id);
        if (timer == null) { return new (default(DateTime)); }

        return new(timer.TriggerTime);
    }
}
