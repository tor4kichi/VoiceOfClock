using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Services.SoundPlayer;

namespace VoiceOfClock.Contract.Services;

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