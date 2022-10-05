using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Markup;
using VoiceOfClock.Models.Domain;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using SystemSpeechSynthesizer = System.Speech.Synthesis.SpeechSynthesizer;
using WindowsSpeechSynthesizer = Windows.Media.SpeechSynthesis.SpeechSynthesizer;
namespace VoiceOfClock.UseCases;

public sealed class VoicePlayer : IApplicationLifeCycleAware,
    IRecipient<TimeOfDayPlayVoiceRequest>
{
    private readonly SystemVoicePlayer _systemVoicePlayer;
    private readonly WindowsVoicePlayer _windowsVoicePlayer;
    private readonly IMessenger _messenger;
    private readonly TimerSettings _timerSettings;

    public VoicePlayer(
        SystemVoicePlayer systemVoicePlayer, 
        WindowsVoicePlayer windowsVoicePlayer,
        IMessenger messenger,
        TimerSettings timerSettings
        )
    {
        _systemVoicePlayer = systemVoicePlayer;
        _windowsVoicePlayer = windowsVoicePlayer;
        _messenger = messenger;
        _timerSettings = timerSettings;
    }

    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);
    }

    void IApplicationLifeCycleAware.Resuming()
    {
    }

    void IApplicationLifeCycleAware.Suspending()
    {
    }

    void IRecipient<TimeOfDayPlayVoiceRequest>.Receive(TimeOfDayPlayVoiceRequest message)
    {
        var request = message.Data;
        string voiceId = _timerSettings.SpeechActorId;        
        if (_windowsVoicePlayer.CanPlayVoice(voiceId))
        {
            message.Reply(_windowsVoicePlayer.PlayVoiceAsync(request, voiceId));
        }
        else if (_systemVoicePlayer.CanPlayVoiceId(voiceId))
        {
            message.Reply(_systemVoicePlayer.PlayVoiceAsync(request, voiceId));
        }
        else
        {
            _timerSettings.SpeechActorId = "";
            message.Reply(_windowsVoicePlayer.PlayVoiceAsync(request));
        }
    }
}

public class SystemVoicePlayer    
{
    private readonly TranslationProcesser _translationProcesser;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SystemSpeechSynthesizer _speechSynthesiser;
    private readonly Dictionary<string, InstalledVoice> _installedVoices;

    public SystemVoicePlayer(
        TranslationProcesser translationProcesser
        )
    {
        _translationProcesser = translationProcesser;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _speechSynthesiser = new SystemSpeechSynthesizer();

        _installedVoices = _speechSynthesiser.GetInstalledVoices().ToDictionary(x => x.VoiceInfo.Id);
    }

    public bool CanPlayVoiceId(string voiceId)
    {
        return _installedVoices.ContainsKey(voiceId);
    }

    public async Task<PlayVoiceResult> PlayVoiceAsync(TimeOfDayPlayVoiceRequestData request, string voiceId = null, double speechRate = 1.0, double speechPitch = 1.0)
    {
        _installedVoices.TryGetValue(voiceId, out var voice);        
        string speechData = _translationProcesser.TranslateTimeOfDay(request.Time);
        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var speakCompletedObservable = Observable.FromEventPattern<SpeakCompletedEventArgs>(
                h => _speechSynthesiser.SpeakCompleted += h,
                h => _speechSynthesiser.SpeakCompleted -= h
                ).Take(1);

            if (voice is not null)
            {
                _speechSynthesiser.SelectVoice(voice.VoiceInfo.Name);
            }

            var prompt = _speechSynthesiser.SpeakAsync(speechData);

            await speakCompletedObservable;

            return PlayVoiceResult.Success();
        });

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

}


public sealed class WindowsVoicePlayer
{    
    private readonly TranslationProcesser _translationProcesser;
    private readonly MediaPlayer _mediaPlayer;
    private readonly DispatcherQueue _dispatcherQueue;

    public WindowsVoicePlayer(
        TranslationProcesser translationProcesser
        )
    {
        _translationProcesser = translationProcesser;
        _mediaPlayer = new MediaPlayer();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _mediaPlayer.AutoPlay = true;
        _mediaPlayer.SourceChanged += _mediaPlayer_SourceChanged;
        _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

        _allVoices = WindowsSpeechSynthesizer.AllVoices.ToDictionary(x => x.Id);
        _currentVoiceInfo = WindowsSpeechSynthesizer.AllVoices.FirstOrDefault(x => x.Language == CultureInfo.CurrentCulture.Name) ?? WindowsSpeechSynthesizer.DefaultVoice;
    }

    private readonly Dictionary<string, VoiceInformation> _allVoices;
    VoiceInformation _currentVoiceInfo;

    public bool CanPlayVoice(string voiceId)
    {
        return _allVoices.ContainsKey(voiceId);
    }

    public async Task<PlayVoiceResult> PlayVoiceAsync(TimeOfDayPlayVoiceRequestData request, string voiceId = null, double speechRate = 1.0, double speechPitch = 1.0)
    {
        _allVoices.TryGetValue(voiceId, out VoiceInformation voiceInfo);
        string speechData = _translationProcesser.TranslateTimeOfDay(request.Time);
        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var stream = await SynthesizeTextToStreamAsync(speechData, voiceInfo);
            PlayOrAddQueueMediaSource(MediaSource.CreateFromStream(stream, stream.ContentType));
            return PlayVoiceResult.Success();
        });
    }

    Task<SpeechSynthesisStream> SynthesizeTextToStreamAsync(string speechData, VoiceInformation voice)
    {
        return Task.Run(async () =>
        {
            using (WindowsSpeechSynthesizer synthesizer = new WindowsSpeechSynthesizer())
            {
                synthesizer.Voice = voice ?? _currentVoiceInfo;
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
    public static PlayVoiceResult Success()
    {
        return new PlayVoiceResult();
    }

    public static PlayVoiceResult Failed()
    {
        return new PlayVoiceResult();
    }
    
    private PlayVoiceResult() { }
}

public sealed class TimeOfDayPlayVoiceRequestData
{
    public DateTime Time { get; set; }
    public TimeOfDayPlayVoiceRequestData(DateTime time)
    {
        Time = time;
    }
}
