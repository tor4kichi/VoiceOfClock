using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.Services.SoundPlayer;

namespace VoiceOfClock.Contracts.Services;

public readonly record struct SoundSourceToken(string Label, SoundSourceType SoundSourceType, string SoundParameter);

public interface ISoundContentPlayerService
{
    IEnumerable<SoundSourceToken> GetAllSoundContents();
    Task PlaySoundContentAsync(in SoundSourceToken token, CancellationToken cancellationToken = default);
    Task PlaySoundContentAsync(SoundSourceType soundSourceType, string soundParameter, CancellationToken cancellationToken = default);
    Task PlayTextAsync(string text, CancellationToken cancellationToken = default);
    Task PlayTextWithSsmlAsync(string ssml, CancellationToken cancellationToken = default);
    Task PlayTimeOfDayAsync(DateTime time, CancellationToken cancellationToken = default);
    Task PlayAudioFileAsync(string filePath, double volume = 1.0, AudioSpan? audioSpan = null, CancellationToken cancellationToken = default);
    Task PlaySystemSoundAsync(WindowsNotificationSoundType soundType, CancellationToken cancellationToken = default);
}


public enum WindowsNotificationSoundType
{
    Default,
    IM,
    Mail,
    Reminder,
    SMS,
    Looping_Alarm,
    Looping_Alarm2,
    Looping_Alarm3,
    Looping_Alarm4,
    Looping_Alarm5,
    Looping_Alarm6,
    Looping_Alarm7,
    Looping_Alarm8,
    Looping_Alarm9,
    Looping_Alarm10,
    Looping_Call,
    Looping_Call2,
    Looping_Call3,
    Looping_Call4,
    Looping_Call5,
    Looping_Call6,
    Looping_Call7,
    Looping_Call8,
    Looping_Call9,
    Looping_Call10,
}
