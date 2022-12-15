using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace VoiceOfClock.Services.SoundPlayer;

public sealed class AudioSoundSourcePlayer : ISoundPlayer
{
    private readonly AudioSoundSourceRepository _audioSoundSourceRepository;

    public AudioSoundSourcePlayer(AudioSoundSourceRepository audioSoundSourceRepository)
    {
        _audioSoundSourceRepository = audioSoundSourceRepository;
    }

    IEnumerable<SoundSourceToken> ISoundPlayer.GetSoundSources()
    {
        return _audioSoundSourceRepository.ReadAllItems()
            .Select(x => new SoundSourceToken(!string.IsNullOrWhiteSpace(x.Title) ? x.Title : Path.GetFileNameWithoutExtension(x.FilePath), SoundSourceType.AudioFile, x.Id.ToString()));            
    }

    SoundSourceType[] ISoundPlayer.SupportedSourceTypes { get; } = new[] { SoundSourceType.AudioFile };

    Task ISoundPlayer.PlayAsync(MediaPlayer mediaPlayer, SoundSourceType soundSourceType, string soundParameter, CancellationToken cancellationToken)
    {
        return PlayAsync(mediaPlayer, int.Parse(soundParameter), cancellationToken);
    }

    public async Task PlayAsync(MediaPlayer mediaPlayer, int soundId, CancellationToken cancellationToken = default)
    {
        var audio = _audioSoundSourceRepository.FindById(soundId);
        if (audio == null)
        {
            throw new ArgumentException($"not found soundId: {soundId}");
        }

        await PlayAsync(mediaPlayer, audio.FilePath, audio.SoundVolume, audio.AudioSpan, cancellationToken);        
    }

    public async Task PlayAsync(MediaPlayer mediaPlayer, string filePath, double volume = 1.0, AudioSpan? audioSpan = null, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNullOrWhiteSpace(filePath);

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(filePath);
            using var source = MediaSource.CreateFromStorageFile(file);
            mediaPlayer.Volume = volume;

            if (audioSpan is not null and AudioSpan realAudioSpan)
            {
                await mediaPlayer.PlayAsync(new MediaPlaybackItem(source, realAudioSpan.Begin, realAudioSpan.End - realAudioSpan.Begin), cancellationToken);
            }
            else
            {
                await mediaPlayer.PlayAsync(source, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("AudioSoundSourcePlayer.PlayAsync() was cancelled.");
        }
    }
}




