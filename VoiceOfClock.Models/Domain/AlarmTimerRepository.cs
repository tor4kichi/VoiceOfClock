using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using VoiceOfClock.Models.Infrastructure;

namespace VoiceOfClock.Models.Domain;

public sealed class AlarmTimerRepository : LiteDBRepositoryBase<AlarmTimerEntity>
{
    public AlarmTimerRepository(ILiteDatabase liteDatabase) : base(liteDatabase)
    {
    }
}

public sealed class AlarmTimerEntity
{
    [BsonId(autoId: true)]
    public Guid Id { get; init; }

    public TimeOnly TimeOfDay { get; set; }

    public DayOfWeek[] EnabledDayOfWeeks { get; set; } = Enum.GetValues<DayOfWeek>();

    public bool IsEnabled { get; set; } = true;

    public string Title { get; set; } = string.Empty;

    public TimeSpan? Snooze { get; set; }

    public SoundSourceType SoundSourceType { get; set; }

    public string SoundContent { get; set; } = string.Empty;
}
