using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Infrastructure;

namespace VoiceOfClock.Models.Domain;

public sealed class OneShotTimerRepository : LiteDBRepositoryBase<OneShotTimerEntity>
{
    public OneShotTimerRepository(ILiteDatabase liteDatabase) : base(liteDatabase)
    {
    }
}

public sealed class OneShotTimerEntity
{
    [BsonId(autoId: true)]
    public Guid Id { get; init; }

    public TimeSpan Time { get; set; }

    public bool IsEnabled { get; set; }

    public string Title { get; set; }

    public SoundSourceType SoundSourceType { get; set; }

    public string SoundContent { get; set; }
}
