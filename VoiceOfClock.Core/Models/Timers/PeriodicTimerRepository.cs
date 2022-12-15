using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Infrastructure;

namespace VoiceOfClock.Core.Models.Timers;

public sealed class PeriodicTimerRepository : LiteDBRepositoryBase<PeriodicTimerEntity>
{
    public PeriodicTimerRepository(ILiteDatabase liteDatabase) : base(liteDatabase)
    {
    }
}

public sealed class PeriodicTimerEntity : ITimer
{
    [BsonId(autoId: true)]
    public Guid Id { get; set; }

    public TimeSpan IntervalTime { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan StartTime { get; set; }
    
    public TimeSpan EndTime { get; set; }

    public bool IsEnabled { get; set; }

    public string Title { get; set; } = string.Empty;

    public DayOfWeek[] EnabledDayOfWeeks { get; set; } = Enum.GetValues<DayOfWeek>();
    
    public int Order { get; set; }

    public SoundSourceType SoundSourceType { get; set; } = SoundSourceType.DateTimeToSpeech;

    public string SoundContent { get; set; } = string.Empty;
}
