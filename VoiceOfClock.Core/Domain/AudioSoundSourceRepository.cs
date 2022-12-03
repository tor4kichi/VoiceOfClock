using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Infrastructure;

namespace VoiceOfClock.Core.Domain;

public sealed class AudioSoundSourceRepository : LiteDBRepositoryBase<AudioSoundSourceEntity>
{
    public AudioSoundSourceRepository(ILiteDatabase liteDatabase) : base(liteDatabase)
    {
    }
}

public sealed class AudioSoundSourceEntity
{
    public AudioSoundSourceEntity()
    {
        FilePath = string.Empty;
    }
    public AudioSoundSourceEntity(string filePath, TimeSpan duration)
    {
        FilePath = filePath;
        Duration = duration;
    }

    [BsonId]
    public int Id { get; init; }

    public string FilePath { get; set; }

    public TimeSpan Duration { get; set; }

    public AudioSpan AudioSpan { get; set; }

    public string? Title { get; set; }

    public double SoundVolume { get; set; } = 1.0;    
}

public record struct AudioSpan(TimeSpan Begin, TimeSpan End);