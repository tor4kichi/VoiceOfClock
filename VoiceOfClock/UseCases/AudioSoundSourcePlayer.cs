using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
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
using VoiceOfClock.Core.Domain;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace VoiceOfClock.UseCases;


public readonly struct PlayAudioRequestResult
{
    public static PlayAudioRequestResult Failed => new PlayAudioRequestResult(false);
    public PlayAudioRequestResult(bool isSuccessed)
    {
        IsSuccessed = isSuccessed;
    }

    public readonly bool IsSuccessed;
}

public sealed class PlayAudioRequestMessage : AsyncRequestMessage<PlayAudioRequestResult>
{
    public PlayAudioRequestMessage(string audioSoundSourceId, CancellationToken cancellationToken = default)
    {
        AudioSoundSourceId = audioSoundSourceId;
        CancellationToken = cancellationToken;
    }

    public string AudioSoundSourceId { get; }
    public CancellationToken CancellationToken { get; }
}

public sealed class AudioSoundSourcePlayer : ObservableRecipient
    , IRecipient<PlayAudioRequestMessage>
    , IApplicationLifeCycleAware
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly AudioSoundSourceRepository _audioSoundSourceRepository;
    private readonly DispatcherQueue _dispatcherQueue;

    public AudioSoundSourcePlayer(AudioSoundSourceRepository audioSoundSourceRepository)
    {
        _mediaPlayer = new MediaPlayer();
        _audioSoundSourceRepository = audioSoundSourceRepository;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _mediaPlayer.SourceChanged += OnMediaPlaylerSourceChanged;
    }

    void IApplicationLifeCycleAware.Initialize()
    {
        base.Messenger.RegisterAll(this);
    }

    void IApplicationLifeCycleAware.Resuming()
    {
        
    }

    void IApplicationLifeCycleAware.Suspending()
    {
        
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

    void IRecipient<PlayAudioRequestMessage>.Receive(PlayAudioRequestMessage message)
    {
        var audio = _audioSoundSourceRepository.FindById(int.Parse(message.AudioSoundSourceId));
        if (audio == null)
        {
            message.Reply(PlayAudioRequestResult.Failed);
            return;
        }

        Guard.IsNotNullOrWhiteSpace(audio.FilePath);

        message.Reply(_dispatcherQueue.EnqueueAsync(async () =>
        {
            var tcs = new TaskCompletionSource();

            if (message.CancellationToken != default)
            {
                message.CancellationToken.Register(() =>
                {
                    tcs!.TrySetCanceled();
                    _mediaPlayer.Pause();
                    _mediaPlayer.Source = null;
                });
            }

            void _mediaPlayler_MediaEnded(MediaPlayer sender, object args)
            {
                tcs.TrySetResult();
            }

            void _mediaPlayler_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
            {
                tcs.SetException(args.ExtendedErrorCode);
            }

            _mediaPlayer.MediaEnded += _mediaPlayler_MediaEnded;
            _mediaPlayer.MediaFailed += _mediaPlayler_MediaFailed;
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(audio.FilePath);
                var source = MediaSource.CreateFromStorageFile(file);
                _mediaPlayer.Volume = audio.SoundVolume;
                if (audio.AudioSpan.End != TimeSpan.Zero)
                {
                    _mediaPlayer.Source = new MediaPlaybackItem(source, audio.AudioSpan.Begin, audio.AudioSpan.End - audio.AudioSpan.Begin);
                    _mediaPlayer.Play();
                }
                else
                {
                    _mediaPlayer.Source = source;
                }

                await tcs.Task;
                return new PlayAudioRequestResult(true);
            }
            catch
            {
                return PlayAudioRequestResult.Failed;
            }
            finally
            {
                _mediaPlayer.MediaEnded -= _mediaPlayler_MediaEnded;
                _mediaPlayer.MediaFailed -= _mediaPlayler_MediaFailed;
            }            
        }));
    }
}
