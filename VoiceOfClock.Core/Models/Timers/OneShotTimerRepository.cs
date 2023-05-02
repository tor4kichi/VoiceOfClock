using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Infrastructure;

namespace VoiceOfClock.Core.Models.Timers;

public sealed class OneShotTimerRepository : LiteDBRepositoryBase<OneShotTimerEntity>
{
    private readonly ILiteStorage<string> _fileStorage;

    public OneShotTimerRepository(ILiteDatabase liteDatabase) : base(liteDatabase)
    {
        _fileStorage = liteDatabase.FileStorage;
    }
    
    public OneShotTimerEntity? GetInstantTimer()
    {
        if (_fileStorage.Exists("InstantOneShotTimer") is false)
        {
            return null;
        }

        try
        {
            using (MemoryStream stream = new MemoryStream())
            {
                _fileStorage.Download("InstantOneShotTimer", stream);
                stream.Position = 0;

                return System.Text.Json.JsonSerializer.Deserialize<OneShotTimerEntity>(stream);
            }
        }
        catch
        {
            return null;
        }
    }

    public void SaveInstantTimer(OneShotTimerEntity entity)
    {
        using (var stream = new MemoryStream())
        {            
            System.Text.Json.JsonSerializer.Serialize(stream , entity);
            stream.Position = 0;
            _fileStorage.Upload("InstantOneShotTimer", "InstantOneShotTimer.json", stream);
        }
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
    public bool IsRunning { get; set; }
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
