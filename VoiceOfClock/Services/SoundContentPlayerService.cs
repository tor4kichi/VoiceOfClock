using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Services.SoundPlayer;
using Windows.Media.Playback;
using MediaPlayer = Windows.Media.Playback.MediaPlayer;

namespace VoiceOfClock.Services;

public sealed class SoundContentPlayerService : ISoundContentPlayerService
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly AudioSoundSourcePlayer _audioSoundSourcePlayer;
    private readonly SystemSoundPlayer _systemSoundPlayer;
    private readonly VoicePlayer _voicePlayer;
    private readonly DispatcherQueue _dispatcherQueue;
    private ISoundPlayer[] _players { get; }
    private readonly Dictionary<SoundSourceType, List<ISoundPlayer>> _playerMap;

    public SoundContentPlayerService(
        MediaPlayer mediaPlayer,
        AudioSoundSourcePlayer audioSoundSourcePlayer,
        SystemSoundPlayer systemSoundPlayer,
        VoicePlayer voicePlayer
        )
    {
        _mediaPlayer = mediaPlayer;
        _audioSoundSourcePlayer = audioSoundSourcePlayer;
        _systemSoundPlayer = systemSoundPlayer;
        _voicePlayer = voicePlayer;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _mediaPlayer.SourceChanged += OnMediaPlaylerSourceChanged;

        _players = new ISoundPlayer[]{
            voicePlayer,
            audioSoundSourcePlayer,
            systemSoundPlayer,
            };

        _playerMap = _players.SelectMany(x => x.SupportedSourceTypes.Select(y => (SoundSourceType: y, SoundPlayer: x)))
            .GroupBy(x => x.SoundSourceType)
            .Select(x => (Key: x.Key, Items: x.Select(x => x.SoundPlayer).ToList()))
            .ToDictionary(x => x.Key, x => x.Items);
    }

    public IEnumerable<SoundSourceToken> GetAllSoundContents()
    {
        return _players.SelectMany(x => x.GetSoundSources());
    }

    
    public IEnumerable<IVoice> GetVoices()
    {
        return _voicePlayer.GetVoices();
    }

    public IVoice? GetVoice(string id)
    {
        return _voicePlayer.GetVoice(id);
    }

    IDisposable? _prevMediaSource;
    private void OnMediaPlaylerSourceChanged(MediaPlayer sender, object args)
    {
        if (_prevMediaSource != null)
        {
            _prevMediaSource.Dispose();
            _prevMediaSource = null;
        }

        if (sender.Source is IDisposable disposable)
        {
            _prevMediaSource = disposable;
        }
        else if (sender.Source is MediaPlaybackItem mpItem)
        {
            _prevMediaSource = mpItem.Source;
        }
    }

    void ResetMediaPlayerParameter()
    {
        _mediaPlayer.Volume = 1.0;
    }

    public Task PlaySoundContentAsync(in SoundSourceToken token, CancellationToken cancellationToken = default)
    {
        return PlaySoundContentAsync(token.SoundSourceType, token.SoundParameter, cancellationToken);
    }

    public Task PlaySoundContentAsync(SoundSourceType soundSourceType, string soundParameter, CancellationToken cancellationToken = default)
    {
        if (_playerMap.TryGetValue(soundSourceType, out List<ISoundPlayer>? players) is false)
        {
            return Task.CompletedTask;
        }

        return _dispatcherQueue.EnqueueAsync(async () =>
        {
            foreach (var player in players)
            {
                ResetMediaPlayerParameter();

                try
                {
                    await player.PlayAsync(_mediaPlayer, soundSourceType, soundParameter, cancellationToken);
                    return;
                }
                catch
                {

                }
            }
        });
    }
    
    public Task PlayTimeOfDayAsync(DateTime time, IVoice? voice = null, CancellationToken cancellationToken = default)
    {
        return _voicePlayer.PlayTimeOfDayAsync(_mediaPlayer, time, cancellationToken);
    }

    public Task PlayTextAsync(string text, IVoice? voice = null, CancellationToken cancellationToken = default)
    {
        return _voicePlayer.PlayTextAsync(_mediaPlayer, text, cancellationToken);
    }

    public Task PlayTextWithSsmlAsync(string ssml, IVoice? voice = null, CancellationToken cancellationToken = default)
    {
        return _voicePlayer.PlayTextWithSsmlAsync(_mediaPlayer, ssml, cancellationToken);
    }

    public Task PlayAudioFileAsync(string filePath, double volume = 1.0, AudioSpan? audioSpan = null, CancellationToken cancellationToken = default)
    {
        return _audioSoundSourcePlayer.PlayAsync(_mediaPlayer, filePath, volume, audioSpan, cancellationToken);
    }

    public Task PlaySystemSoundAsync(WindowsNotificationSoundType soundType, CancellationToken cancellationToken = default)
    {
        return _systemSoundPlayer.PlayAsync(_mediaPlayer, soundType, cancellationToken);
    }
}

interface ISoundPlayer 
{
    IEnumerable<SoundSourceToken> GetSoundSources();
    SoundSourceType[] SupportedSourceTypes { get; }
    Task PlayAsync(MediaPlayer mediaPlayer, SoundSourceType soundSourceType, string soundParameter, CancellationToken ct);
}

static class SoundPlayerHelper
{
    public static async Task PlayAsync(this MediaPlayer mediaPlayer, IMediaPlaybackSource mediaSource, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();

        if (ct != default)
        {
            ct.Register(() =>
            {
                tcs!.TrySetCanceled();
                mediaPlayer.Pause();
                mediaPlayer.Source = null;
            });
        }

        void _mediaPlayler_MediaEnded(MediaPlayer sender, object args)
        {
            tcs.TrySetResult();
        }

        void _mediaPlayler_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            tcs.TrySetException(args.ExtendedErrorCode);
        }

        mediaPlayer.MediaEnded += _mediaPlayler_MediaEnded;
        mediaPlayer.MediaFailed += _mediaPlayler_MediaFailed;
        try
        {
            mediaPlayer.Source = mediaSource;
            if (mediaPlayer.AutoPlay is false)
            {
                mediaPlayer.Play();
            }

            await tcs.Task;
        }            
        finally
        {
            mediaPlayer.MediaEnded -= _mediaPlayler_MediaEnded;
            mediaPlayer.MediaFailed -= _mediaPlayler_MediaFailed;
        }
    }
}




