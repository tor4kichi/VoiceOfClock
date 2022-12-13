using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Domain;
using VoiceOfClock.Core.Infrastructure;

namespace VoiceOfClock.Core.Domain;

public sealed class OneShotTimerRepository : LiteDBRepositoryBase<OneShotTimerEntity>
{
    public OneShotTimerRepository(ILiteDatabase liteDatabase) : base(liteDatabase)
    {
    }
}

public sealed class OneShotTimerRunningRepository : LiteDBRepositoryBase<OneShotTimerRunningEntity>
{
    public OneShotTimerRunningRepository(ILiteDatabase liteDatabase) : base(liteDatabase)
    {
        
    }
}

public sealed class OneShotTimerRunningEntity
{
    [BsonId(autoId: false)]
    public Guid Id { get; init; }

    public TimeSpan Time { get; set; }
}

public sealed class OneShotTimerEntity : ITimer
{
    [BsonId(autoId: true)]
    public Guid Id { get; init; }

    public TimeSpan Time { get; set; }

    public string Title { get; set; } = string.Empty;

    [BsonField("SoundType")]
    public SoundSourceType SoundSourceType { get; set; }

    [BsonField("SoundParameter")]
    public string SoundContent { get; set; } = string.Empty;
    
    public int Order { get; set; }
}
