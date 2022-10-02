using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using I18NPortable;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;


namespace VoiceOfClock.UseCases
{
    public sealed class UWP_VoicePlayer : IApplicationLifeCycleAware,
        IRecipient<TimeOfDayPlayVoiceRequest>
    {
        private readonly IMessenger _messenger;
        private readonly TranslationProcesser _translationProcesser;
        private readonly MediaPlayer _mediaPlayer;
        private readonly DispatcherQueue _dispatcherQueue;

        public UWP_VoicePlayer(
            IMessenger messenger,
            TranslationProcesser translationProcesser
            )
        {
            _messenger = messenger;
            _translationProcesser = translationProcesser;
            _mediaPlayer = new MediaPlayer();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _mediaPlayer.AutoPlay = true;
            _mediaPlayer.SourceChanged += _mediaPlayer_SourceChanged;
            _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;            
        }

        VoiceInformation _currentVoiceInfo;

        void IApplicationLifeCycleAware.Initialize()
        {
            _messenger.RegisterAll(this);

            _currentVoiceInfo = SpeechSynthesizer.AllVoices.FirstOrDefault(x => x.Language == CultureInfo.CurrentCulture.Name) ?? SpeechSynthesizer.DefaultVoice;
        }

        void IApplicationLifeCycleAware.Resuming()
        {
            
        }

        void IApplicationLifeCycleAware.Suspending()
        {
            
        }


        void IRecipient<TimeOfDayPlayVoiceRequest>.Receive(TimeOfDayPlayVoiceRequest message)
        {
            var data = message.Data;
            string speechData = _translationProcesser.TranslateTimeOfDay(data.Time);
            message.Reply(_dispatcherQueue.EnqueueAsync(async () =>
            {
                var stream = await SynthesizeTextToStreamAsync(speechData);
                PlayOrAddQueueMediaSource(MediaSource.CreateFromStream(stream, stream.ContentType));                
                return new PlayVoiceResult();
            }));            
        }                


        Task<SpeechSynthesisStream> SynthesizeTextToStreamAsync(string speechData)
        {
            return Task.Run(async () =>
            {
                using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
                {
                    synthesizer.Voice = _currentVoiceInfo;
                    return await synthesizer.SynthesizeTextToStreamAsync(speechData);
                }
            });
        }

        void PlayOrAddQueueMediaSource(IMediaPlaybackSource source)
        {
            if (_mediaPlayer.Source != null)
            {
                _voicesQueue.Enqueue(source);
            }
            else
            {
                _mediaPlayer.Source = source;                
            }            
        }


        //async ValueTask PlayOrAddQueueMediaSource(MediaSource source)
        //{
        //    var audioGraphResult = await AudioGraph.CreateAsync(new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Alerts));
        //    if (audioGraphResult.Status == AudioGraphCreationStatus.Success)
        //    {
        //        TaskCompletionSource<long> tcs = new TaskCompletionSource<long>();
        //        using var graph = audioGraphResult.Graph;
        //        var inputNodeResult = await graph.CreateMediaSourceAudioInputNodeAsync(source);
        //        var outputNodeResult = await graph.CreateDeviceOutputNodeAsync();

        //        inputNodeResult.Node.AddOutgoingConnection(outputNodeResult.DeviceOutputNode);
        //        inputNodeResult.Node.MediaSourceCompleted += (s, e) => { tcs.SetResult(0); };

        //        graph.Start();

        //        await tcs.Task;
        //    }
        //    else
        //    {

        //    }
        //}

        MediaPlaybackState _mediaPlaybackState = MediaPlaybackState.None;
        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            Debug.WriteLine($"PlaybackState: {sender.PlaybackState}");
            if (_mediaPlaybackState is MediaPlaybackState.Playing 
                && sender.PlaybackState is MediaPlaybackState.Paused or MediaPlaybackState.None
                )
            {
                if (_voicesQueue.TryDequeue(out var voice))
                {
                    _mediaPlayer.Source = voice;
                }
                else
                {
                    _mediaPlayer.Source = null;
                }
            }

            _mediaPlaybackState = sender.PlaybackState;
        }

        Queue<IMediaPlaybackSource> _voicesQueue = new Queue<IMediaPlaybackSource>();
        IMediaPlaybackSource _prevPlaybackSource;
        private void _mediaPlayer_SourceChanged(MediaPlayer sender, object args)
        {
            if (_prevPlaybackSource != null)
            {
                (_prevPlaybackSource as IDisposable).Dispose();
                _prevPlaybackSource = null;
            }

            _prevPlaybackSource = sender.Source;
        }
    }

    public sealed class TimeOfDayPlayVoiceRequest : AsyncRequestMessage<PlayVoiceResult>
    {
        public TimeOfDayPlayVoiceRequest(TimeOfDayPlayVoiceRequestData data)
        {
            Data = data;
        }

        public TimeOfDayPlayVoiceRequestData Data { get; }
    }

    public sealed class PlayVoiceResult
    {

    }

    public sealed class TimeOfDayPlayVoiceRequestData
    {
        public DateTime Time { get; set; }
        public TimeOfDayPlayVoiceRequestData(DateTime time)
        {
            Time = time;
        }
    }
}
