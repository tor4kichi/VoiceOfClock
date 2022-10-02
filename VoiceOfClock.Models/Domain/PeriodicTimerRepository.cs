using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Infrastructure;

namespace VoiceOfClock.Models.Domain;

public sealed class PeriodicTimerRepository : LiteDBRepositoryBase<PeriodicTimerEntity>
{
    public PeriodicTimerRepository(ILiteDatabase liteDatabase) : base(liteDatabase)
    {
    }
}

public sealed class PeriodicTimerEntity
{
    [BsonId(autoId: true)]
    public Guid Id { get; set; }

    public TimeSpan IntervalTime { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan StartTime { get; set; }
    
    public TimeSpan EndTime { get; set; }

    public bool IsEnabled { get; set; }

    public string Title { get; set; }
}
