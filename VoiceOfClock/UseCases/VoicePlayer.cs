using CommunityToolkit.Diagnostics;
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
    IRecipient<TimeOfDayPlayVoiceRequest>,
    IRecipient<TextPlayVoiceRequest>,
    IRecipient<SsmlPlayVoiceRequest>
{
    private readonly SystemVoicePlayer _systemVoicePlayer;
    private readonly WindowsVoicePlayer _windowsVoicePlayer;
    private readonly TranslationProcesser _translationProcesser;
    private readonly IMessenger _messenger;
    private readonly TimerSettings _timerSettings;
    private readonly IVoicePlayer[] _supportedVoicePlayers;

    private IVoicePlayer FallbackVoicePlayer => _windowsVoicePlayer;

    public VoicePlayer(
        SystemVoicePlayer systemVoicePlayer, 
        WindowsVoicePlayer windowsVoicePlayer,
        TranslationProcesser translationProcesser,
        IMessenger messenger,
        TimerSettings timerSettings
        )
    {
        _systemVoicePlayer = systemVoicePlayer;
        _windowsVoicePlayer = windowsVoicePlayer;
        _translationProcesser = translationProcesser;
        _messenger = messenger;
        _timerSettings = timerSettings;

        _supportedVoicePlayers = new IVoicePlayer[]
        {
            _windowsVoicePlayer,
            _systemVoicePlayer
        };
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

        string? voiceId = _timerSettings.SpeechActorId;
        IVoicePlayer? currentVoicePlayer = _supportedVoicePlayers.FirstOrDefault(x => x.CanPlayVoice(voiceId));
        if (currentVoicePlayer is null)
        {
            _timerSettings.SpeechActorId = string.Empty;
            currentVoicePlayer = FallbackVoicePlayer;
            voiceId = FallbackVoicePlayer.GetAllVoiceId().First();
        }
        

        string voiceLanguage = currentVoicePlayer.SetVoice(voiceId);
        CultureInfo cutureInfo = CultureInfo.GetCultureInfo(voiceLanguage);

        _translationProcesser.SetLocale(voiceLanguage);

        if (_timerSettings.UseSsml)
        {
            static string ToSsmlTimeFormat_HM(DateTime dateTime, bool is24h, IFormatProvider formatProvider, AMPMPosition ampmPosition)
            {
                if (is24h)
                {
                    return $"<say-as interpret-as=\"time\" format=\"hms24\">{dateTime:H:m}</say-as>";
                }
                else
                {
                    if (ampmPosition == AMPMPosition.Prefix)
                    {
                        var ampm = dateTime.ToString("tt", formatProvider);
                        return $"<say-as interpret-as=\"time\" format=\"hms12\">{ampm}{dateTime:h:m}</say-as>";
                    }
                    else if (ampmPosition == AMPMPosition.Postfix)
                    {
                        var ampm = dateTime.ToString("tt", formatProvider);
                        return $"<say-as interpret-as=\"time\" format=\"hms12\">{dateTime:h:m}{ampm}</say-as>";
                    }
                    else
                    {
                        var timeWithAmpm = dateTime.ToString("t", formatProvider);
                        return $"<say-as interpret-as=\"time\" format=\"hms12\">{timeWithAmpm}</say-as>";
                    }
                }
            }
            
            if (currentVoicePlayer is SystemVoicePlayer)
            {
                string speechData = SsmlHelpers.ToSsml1_0Format(_translationProcesser.Translate("TimeOfDayToSpeechText", ToSsmlTimeFormat_HM(request.Time, _timerSettings.IsTimeSpeechWith24h, cutureInfo.DateTimeFormat, _timerSettings.GetAmpmPosition(cutureInfo))), _timerSettings.SpeechRate, _timerSettings.SpeechPitch, voiceLanguage);
                message.Reply(currentVoicePlayer.PlayVoiceWithSsmlAsync(speechData, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume));
            }
            else if (currentVoicePlayer is WindowsVoicePlayer)
            {
                string speechData = SsmlHelpers.ToSsml1_1Format(_translationProcesser.Translate("TimeOfDayToSpeechText", ToSsmlTimeFormat_HM(request.Time, _timerSettings.IsTimeSpeechWith24h, cutureInfo.DateTimeFormat, _timerSettings.GetAmpmPosition(cutureInfo))), _timerSettings.SpeechRate, _timerSettings.SpeechPitch, voiceLanguage);
                message.Reply(currentVoicePlayer.PlayVoiceWithSsmlAsync(speechData, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume));
            }            
        }
        else
        {
            string speechData = _translationProcesser.Translate("TimeOfDayToSpeechText", _translationProcesser.TranslateTimeOfDay(request.Time, _timerSettings.IsTimeSpeechWith24h));
            message.Reply(currentVoicePlayer.PlayVoiceWithTextAsync(speechData, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume));
        }
    }

    void IRecipient<TextPlayVoiceRequest>.Receive(TextPlayVoiceRequest message)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            message.Reply(PlayVoiceResult.Failed());
        }

        string? voiceId = _timerSettings.SpeechActorId;
        var currentVoicePlayer = _supportedVoicePlayers.FirstOrDefault(x => x.CanPlayVoice(voiceId));
        if (currentVoicePlayer is null)
        {
            _timerSettings.SpeechActorId = "";
            currentVoicePlayer = FallbackVoicePlayer;
            voiceId = null;
        }

        message.Reply(currentVoicePlayer.PlayVoiceWithTextAsync(message.Text, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume));
    }

    void IRecipient<SsmlPlayVoiceRequest>.Receive(SsmlPlayVoiceRequest message)
    {        
        if (string.IsNullOrWhiteSpace(message.Ssml))
        {
            message.Reply(PlayVoiceResult.Failed());
        }

        string? voiceId = _timerSettings.SpeechActorId;
        var currentVoicePlayer = _supportedVoicePlayers.FirstOrDefault(x => x.CanPlayVoice(voiceId));
        if (currentVoicePlayer is null)
        {
            _timerSettings.SpeechActorId = "";
            currentVoicePlayer = FallbackVoicePlayer;
            voiceId = null;
        }

        string voiceLanguage = currentVoicePlayer.SetVoice(voiceId);
        CultureInfo cutureInfo = CultureInfo.GetCultureInfo(voiceLanguage);

        _translationProcesser.SetLocale(voiceLanguage);

        message.Reply(currentVoicePlayer.PlayVoiceWithSsmlAsync(message.Ssml, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume));
    }
}

public interface IVoicePlayer
{
    IReadOnlyCollection<string> GetAllVoiceId();
    bool CanPlayVoice(string? voiceId);
    string SetVoice(string? voiceId);
    Task<PlayVoiceResult> PlayVoiceWithSsmlAsync(string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1);
    Task<PlayVoiceResult> PlayVoiceWithTextAsync(string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1);
}

public class SystemVoicePlayer : IVoicePlayer
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SystemSpeechSynthesizer _speechSynthesiser;
    private readonly Dictionary<string, InstalledVoice> _installedVoices;

    public SystemVoicePlayer()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _speechSynthesiser = new SystemSpeechSynthesizer();

        _installedVoices = _speechSynthesiser.GetInstalledVoices().Where(x => x.Enabled).ToDictionary(x => x.VoiceInfo.Id);
    }

    public bool CanPlayVoice(string? voiceId)
    {
        return _installedVoices.ContainsKey(voiceId ?? string.Empty);
    }

    public IReadOnlyCollection<string> GetAllVoiceId()
    {
        return _installedVoices.Keys;
    }

    public string SetVoice(string? voiceId)
    {
        if(_installedVoices.TryGetValue(voiceId ?? string.Empty, out var voice) is false)
        {
            voice = _installedVoices.First().Value;
        }

        _speechSynthesiser.SelectVoice(voice.VoiceInfo.Name);
        return voice.VoiceInfo.Culture.Name;
    }

    public async Task<PlayVoiceResult> PlayVoiceWithTextAsync(string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1)
    {
        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var speakCompletedObservable = Observable.FromEventPattern<SpeakCompletedEventArgs>(
                h => _speechSynthesiser.SpeakCompleted += h,
                h => _speechSynthesiser.SpeakCompleted -= h
                ).Take(1);

            SetupSpeechSynsesiser(speechRate, speechPitch, speechVolume);
            var p = _speechSynthesiser.SpeakAsync(content);

            await speakCompletedObservable;

            return PlayVoiceResult.Success();
        });
    }



    public async Task<PlayVoiceResult> PlayVoiceWithSsmlAsync(string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1)
    {
        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var speakCompletedObservable = Observable.FromEventPattern<SpeakCompletedEventArgs>(
                h => _speechSynthesiser.SpeakCompleted += h,
                h => _speechSynthesiser.SpeakCompleted -= h
                ).Take(1);

            SetupSpeechSynsesiser(speechRate, speechPitch, speechVolume);

            var p = _speechSynthesiser.SpeakSsmlAsync(content);

            await speakCompletedObservable;

            return PlayVoiceResult.Success();
        });
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:未使用のパラメーターを削除します", Justification = "<保留中>")]
    private void SetupSpeechSynsesiser(double speechRate, double speechPitch, double speechVolume)
    {
        // Rate 
        // -10 ~ 10
        if (speechRate < 1.0)
        {
            // 0.5を-10 0.9を -2   0.0 を0
            _speechSynthesiser.Rate = -(int)Math.Floor((1.0 - speechRate) * 20);
        }
        else if (speechRate > 1.0)
        {
            // 2.0 を10 1.5 を5 
            _speechSynthesiser.Rate = Math.Clamp((int)Math.Floor((speechRate - 1.0) * 10), -10, 10);
        }
        
        // Volume
        // 0 ~ 100
        // https://learn.microsoft.com/ja-jp/dotnet/api/system.speech.synthesis.speechsynthesizer.volume?view=netframework-4.8
        _speechSynthesiser.Volume = Math.Clamp((int)(speechVolume * 100), 0, 100);
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



public sealed class WindowsVoicePlayer : IVoicePlayer
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly DispatcherQueue _dispatcherQueue;

    public WindowsVoicePlayer(
        )
    {
        _mediaPlayer = new MediaPlayer();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _mediaPlayer.AutoPlay = true;
        _mediaPlayer.SourceChanged += OnMediaPlayerSourceChanged;
        _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

        _allVoices = WindowsSpeechSynthesizer.AllVoices.ToDictionary(x => x.Id);
        _currentVoiceInfo = WindowsSpeechSynthesizer.AllVoices.FirstOrDefault(x => x.Language == CultureInfo.CurrentCulture.Name) ?? WindowsSpeechSynthesizer.DefaultVoice;
    }

    private readonly Dictionary<string, VoiceInformation> _allVoices;
    VoiceInformation _currentVoiceInfo;

    private readonly Dictionary<MediaSource, TaskCompletionSource> _waitingHandles = new();
    public bool CanPlayVoice(string? voiceId)
    {
        return _allVoices.ContainsKey(voiceId ?? string.Empty);
    }

    public IReadOnlyCollection<string> GetAllVoiceId()
    {
        return _allVoices.Keys;
    }

    public string SetVoice(string? voiceId)
    {
        if (_allVoices.TryGetValue(voiceId ?? string.Empty, out var voice) is false)
        {
            voice = _allVoices.First().Value;
        }

        _currentVoiceInfo = voice;
        return _currentVoiceInfo.Language;
    }

    public async Task<PlayVoiceResult> PlayVoiceWithSsmlAsync(string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1)
    {
        var stream = await Task.Run(async () =>
        {
            using WindowsSpeechSynthesizer synthesizer = new ();
            synthesizer.Voice = _currentVoiceInfo;
            synthesizer.Options.AudioVolume = Math.Clamp(speechVolume, 0, 1);
            synthesizer.Options.AppendedSilence = SpeechAppendedSilence.Min;
            synthesizer.Options.PunctuationSilence = SpeechPunctuationSilence.Min;
            return await synthesizer.SynthesizeSsmlToStreamAsync(content);
        });

        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            await PlayMediaSourceAsync(MediaSource.CreateFromStream(stream, stream.ContentType));
            return PlayVoiceResult.Success();
        });
    }


    public async Task<PlayVoiceResult> PlayVoiceWithTextAsync(string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1)
    {
        var stream = await Task.Run(async () =>
        {
            // https://learn.microsoft.com/ja-jp/uwp/api/windows.media.speechsynthesis.speechsynthesizeroptions?view=winrt-22621#properties
            using WindowsSpeechSynthesizer synthesizer = new ();
            synthesizer.Voice = _currentVoiceInfo;
            synthesizer.Options.AudioVolume = Math.Clamp(speechVolume, 0, 1);
            synthesizer.Options.SpeakingRate = Math.Clamp(speechRate, 0.5, 6.0);
            synthesizer.Options.AudioPitch = Math.Clamp(speechPitch, 0.0, 2.0);
            synthesizer.Options.AppendedSilence = SpeechAppendedSilence.Min;
            synthesizer.Options.PunctuationSilence = SpeechPunctuationSilence.Min;
            return await synthesizer.SynthesizeTextToStreamAsync(content);
        });

        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            await PlayMediaSourceAsync(MediaSource.CreateFromStream(stream, stream.ContentType));
            return PlayVoiceResult.Success();
        });
    }


    Task PlayMediaSourceAsync(MediaSource source)
    {
        var cts = new TaskCompletionSource();
        _waitingHandles.Add(source, cts);
        if (_mediaPlayer.Source != null)
        {
            _voicesQueue.Enqueue(source);
        }
        else
        {
            _mediaPlayer.Source = source;
        }

        return cts.Task;
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
            if (_mediaPlayer.Source is MediaSource prev)
            {
                if (_waitingHandles.Remove(prev, out var cts))
                {
                    cts.SetResult();
                }
            }

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

    private readonly Queue<MediaSource> _voicesQueue = new ();
    private IDisposable? _prevPlaybackSource;
    private void OnMediaPlayerSourceChanged(MediaPlayer sender, object args)
    {
        if (_prevPlaybackSource != null)
        {
            _prevPlaybackSource.Dispose();
            _prevPlaybackSource = null;
        }

        _prevPlaybackSource = sender.Source as IDisposable;
    }
}

public sealed class TimeOfDayPlayVoiceRequest : AsyncRequestMessage<PlayVoiceResult>
{
    public TimeOfDayPlayVoiceRequest(DateTime data)
    {
        Data = new TimeOfDayPlayVoiceRequestData(data);
    }

    public TimeOfDayPlayVoiceRequestData Data { get; }
}

public sealed class TextPlayVoiceRequest : AsyncRequestMessage<PlayVoiceResult>
{
    public TextPlayVoiceRequest(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

public sealed class SsmlPlayVoiceRequest : AsyncRequestMessage<PlayVoiceResult>
{
    public SsmlPlayVoiceRequest(string ssml)
    {
        Ssml = ssml;
    }

    public string Ssml { get; }
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
