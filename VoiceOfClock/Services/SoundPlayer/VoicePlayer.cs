using CommunityToolkit.WinUI;
using I18NPortable;
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
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using SystemSpeechSynthesizer = System.Speech.Synthesis.SpeechSynthesizer;
using WindowsSpeechSynthesizer = Windows.Media.SpeechSynthesis.SpeechSynthesizer;

namespace VoiceOfClock.Services.SoundPlayer;

public sealed class VoicePlayer : ISoundPlayer
{
    private readonly SystemVoicePlayer _systemVoicePlayer;
    private readonly WindowsVoicePlayer _windowsVoicePlayer;
    private readonly TranslationProcesser _translationProcesser;
    private readonly TimerSettings _timerSettings;
    private readonly IVoicePlayer[] _supportedVoicePlayers;

    private IVoicePlayer FallbackVoicePlayer => _windowsVoicePlayer;

    public VoicePlayer(
        SystemVoicePlayer systemVoicePlayer, 
        WindowsVoicePlayer windowsVoicePlayer,
        TranslationProcesser translationProcesser,
        TimerSettings timerSettings
        )
    {
        _systemVoicePlayer = systemVoicePlayer;
        _windowsVoicePlayer = windowsVoicePlayer;
        _translationProcesser = translationProcesser;
        _timerSettings = timerSettings;

        _supportedVoicePlayers = new IVoicePlayer[]
        {
            _windowsVoicePlayer,
            _systemVoicePlayer
        };
    }

    IEnumerable<SoundSourceToken> ISoundPlayer.GetSoundSources()
    {
        yield return new SoundSourceToken(SoundSourceType.Tts.Translate(), SoundSourceType.Tts, string.Empty);
    }

    public IEnumerable<IVoice> GetVoices()
    {
        return _supportedVoicePlayers.SelectMany(x => x.GetVoices());
    }

    public IVoice? GetVoice(string? id)
    {
        if (id == null) { return null; }

        foreach (var player in _supportedVoicePlayers)
        {
            var voice = player.GetVoices().FirstOrDefault(x => x.Id == id);
            if (voice is not null) { return voice; }
        }

        return null;
    }


    SoundSourceType[] ISoundPlayer.SupportedSourceTypes { get; } = new[] 
    {
        SoundSourceType.Tts,
        SoundSourceType.TtsWithSSML,
        SoundSourceType.DateTimeToSpeech
    }; 

    async Task ISoundPlayer.PlayAsync(MediaPlayer mediaPlayer, SoundSourceType soundSourceType, string soundParameter, CancellationToken ct)
    {
        if (soundSourceType == SoundSourceType.DateTimeToSpeech)            
        {
            if (DateTime.TryParse(soundParameter, out DateTime time))
            {
                await PlayTimeOfDayAsync(mediaPlayer, time, ct);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }   
        else if (soundSourceType == SoundSourceType.Tts)
        {
            await PlayTextAsync(mediaPlayer, soundParameter, ct);
        }
        else if (soundSourceType == SoundSourceType.TtsWithSSML)
        {
            await PlayTextWithSsmlAsync(mediaPlayer, soundParameter, ct);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public Task<bool> PlayTimeOfDayAsync(MediaPlayer mediaPlayer, DateTime time, CancellationToken ct, IVoice? voice = null)
    {
        var (voicePlayer, selectedVoice) = GetEnsureVoiceAndPlayer(voice);
        string voiceLanguage = selectedVoice.Language;
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

            // SSML使用時はレートとピッチの指定をSSML内（SsmlHelpers.ToSsml1_0Formatなど）で指定しているのでPlay側では指定をスキップしている
            if (voicePlayer is SystemVoicePlayer)
            {
                string speechData = SsmlHelpers.ToSsml1_0Format(_translationProcesser.Translate("TimeOfDayToSpeechText", ToSsmlTimeFormat_HM(time, _timerSettings.IsTimeSpeechWith24h, cutureInfo.DateTimeFormat, _timerSettings.GetAmpmPosition(cutureInfo))), _timerSettings.SpeechRate, _timerSettings.SpeechPitch, voiceLanguage);
                return voicePlayer.PlayVoiceWithSsmlAsync(mediaPlayer, selectedVoice, speechData, speechVolume: _timerSettings.SpeechVolume, ct: ct);
            }
            else if (voicePlayer is WindowsVoicePlayer)
            {
                string speechData = SsmlHelpers.ToSsml1_1Format(_translationProcesser.Translate("TimeOfDayToSpeechText", ToSsmlTimeFormat_HM(time, _timerSettings.IsTimeSpeechWith24h, cutureInfo.DateTimeFormat, _timerSettings.GetAmpmPosition(cutureInfo))), _timerSettings.SpeechRate, _timerSettings.SpeechPitch, voiceLanguage);
                return voicePlayer.PlayVoiceWithSsmlAsync(mediaPlayer, selectedVoice, speechData, speechVolume: _timerSettings.SpeechVolume, ct: ct);
            }
            else
            {
                throw new NotSupportedException("VoicePlayer not found.");
            }
        }
        else
        {
            string speechData = _translationProcesser.Translate("TimeOfDayToSpeechText", _translationProcesser.TranslateTimeOfDay(time, _timerSettings.IsTimeSpeechWith24h));
            return voicePlayer.PlayVoiceWithTextAsync(mediaPlayer, selectedVoice, speechData, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume, ct);
        }
    }

    (IVoicePlayer voicePlayer, IVoice voice) GetEnsureVoiceAndPlayer(IVoice? voice)
    {
        if (voice == null)
        {
            voice = GetVoice(_timerSettings.SpeechActorId);
        }
        if (voice == null)
        {
            _timerSettings.SpeechActorId = "";
            voice = FallbackVoicePlayer.GetFallbackVoice();
        }

        var currentVoicePlayer = _supportedVoicePlayers.FirstOrDefault(x => x.CanPlayVoice(voice));
        if (currentVoicePlayer is null)
        {            
            currentVoicePlayer = FallbackVoicePlayer;
            voice = FallbackVoicePlayer.GetFallbackVoice();
        }

        return (currentVoicePlayer, voice);
    }


    public Task<bool> PlayTextAsync(MediaPlayer mediaPlayer, string text, CancellationToken cancellationToken, IVoice? voice = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(false);
        }

        var (voicePlayer, selectedVoice) = GetEnsureVoiceAndPlayer(voice);

        return voicePlayer.PlayVoiceWithTextAsync(mediaPlayer, selectedVoice, text, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume, cancellationToken);
    }

    public Task<bool> PlayTextWithSsmlAsync(MediaPlayer mediaPlayer, string text, CancellationToken cancellationToken, IVoice? voice = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(false);
        }

        var (voicePlayer, selectedVoice) = GetEnsureVoiceAndPlayer(voice);
        CultureInfo cutureInfo = CultureInfo.GetCultureInfo(selectedVoice.Language);
        _translationProcesser.SetLocale(selectedVoice.Language);

        return voicePlayer.PlayVoiceWithSsmlAsync(mediaPlayer, selectedVoice, text, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume, cancellationToken);
    }


}

public interface IVoicePlayer
{
    IEnumerable<IVoice> GetVoices();
    bool CanPlayVoice(IVoice voice);    
    IVoice GetFallbackVoice();
    Task<bool> PlayVoiceWithSsmlAsync(MediaPlayer mediaPlayer, IVoice voice, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default);
    Task<bool> PlayVoiceWithTextAsync(MediaPlayer mediaPlayer, IVoice voice, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default);
}

public class SystemVoicePlayer : IVoicePlayer
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SystemSpeechSynthesizer _speechSynthesiser;
    private readonly Dictionary<string, LegacyVoiceInformation> _installedVoices;

    public SystemVoicePlayer()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _speechSynthesiser = new SystemSpeechSynthesizer();
        
        _installedVoices = _speechSynthesiser.GetInstalledVoices().Where(x => x.Enabled).Select(x => new LegacyVoiceInformation(x.VoiceInfo)).ToDictionary(x => x.Id);
    }

    public bool CanPlayVoice(IVoice voice)
    {
        return _installedVoices.ContainsKey(voice.Id);
    }

    public IEnumerable<IVoice> GetVoices()
    {
        return _installedVoices.Values;
    }

    public IVoice GetFallbackVoice()
    {
        return _installedVoices.Values.FirstOrDefault() ?? throw new InvalidOperationException();
    }

    public async Task<bool> PlayVoiceWithTextAsync(MediaPlayer mediaPlayer, IVoice voice, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default)
    {
        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var speakCompletedObservable = Observable.FromEventPattern<SpeakCompletedEventArgs>(
                h => _speechSynthesiser.SpeakCompleted += h,
                h => _speechSynthesiser.SpeakCompleted -= h
                ).Take(1);

            _speechSynthesiser.SelectVoice(voice.Name);
            SetupSpeechSynsesiser(speechRate, speechPitch, speechVolume);
            var p = _speechSynthesiser.SpeakAsync(content);

            if (ct != default)
            {
                ct.Register(() =>
                {
                    _speechSynthesiser.SpeakAsyncCancel(p);
                });
            }

            await speakCompletedObservable;

            return true;
        });
    }



    public async Task<bool> PlayVoiceWithSsmlAsync(MediaPlayer mediaPlayer, IVoice voice, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default)
    {
        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var speakCompletedObservable = Observable.FromEventPattern<SpeakCompletedEventArgs>(
                h => _speechSynthesiser.SpeakCompleted += h,
                h => _speechSynthesiser.SpeakCompleted -= h
                ).Take(1);

            _speechSynthesiser.SelectVoice(voice.Name);
            SetupSpeechSynsesiser(speechRate, speechPitch, speechVolume);
            var p = _speechSynthesiser.SpeakSsmlAsync(content);

            if (ct != default)
            {
                ct.Register(() => 
                {
                    _speechSynthesiser.SpeakAsyncCancel(p);
                });
            }
            
            await speakCompletedObservable;

            return true;
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
}

public class LegacyVoiceInformation : IVoice
{
    private readonly VoiceInfo _voiceInfo;

    public LegacyVoiceInformation(VoiceInfo voiceInfo)
    {
        _voiceInfo = voiceInfo;
    }

    public string Id => _voiceInfo.Id;

    public string Name => _voiceInfo.Name;

    public string Language => _voiceInfo.Culture.Name;

    public string Gender => _voiceInfo.Gender.ToString();

    public override string ToString()
    {
        return $"{Name} ({Language})";
    }
}

public sealed class WindowsVoicePlayer : IVoicePlayer
{
    private readonly DispatcherQueue _dispatcherQueue;

    public WindowsVoicePlayer()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _allVoices = WindowsSpeechSynthesizer.AllVoices.Select(x => new WindowsVoice(x)).ToDictionary(x => x.Id);
    }

    private readonly Dictionary<string, WindowsVoice> _allVoices;

    private readonly Dictionary<MediaSource, TaskCompletionSource> _waitingHandles = new();
    public bool CanPlayVoice(IVoice voice)
    {
        return _allVoices.ContainsKey(voice.Id);
    }

    public IEnumerable<IVoice> GetVoices()
    {
        return _allVoices.Values;
    }

    public IVoice GetFallbackVoice()
    {
        return _allVoices.Values.FirstOrDefault() ?? throw new InvalidOperationException();
    }

    public async Task<bool> PlayVoiceWithSsmlAsync(MediaPlayer mediaPlayer, IVoice voice, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default)
    {
        var stream = await Task.Run(async () =>
        {
            using WindowsSpeechSynthesizer synthesizer = new ();
            synthesizer.Voice = ((WindowsVoice)voice).VoiceInfomation;
            synthesizer.Options.AudioVolume = Math.Clamp(speechVolume, 0, 1);
            synthesizer.Options.AppendedSilence = SpeechAppendedSilence.Min;
            synthesizer.Options.PunctuationSilence = SpeechPunctuationSilence.Min;
            return await synthesizer.SynthesizeSsmlToStreamAsync(content);
        });

        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            await mediaPlayer.PlayAsync(MediaSource.CreateFromStream(stream, stream.ContentType), ct);
            return true;
        });
    }


    public async Task<bool> PlayVoiceWithTextAsync(MediaPlayer mediaPlayer, IVoice voice, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default)
    {
        var stream = await Task.Run(async () =>
        {
            // https://learn.microsoft.com/ja-jp/uwp/api/windows.media.speechsynthesis.speechsynthesizeroptions?view=winrt-22621#properties
            using WindowsSpeechSynthesizer synthesizer = new ();
            synthesizer.Voice = ((WindowsVoice)voice).VoiceInfomation;
            synthesizer.Options.AudioVolume = Math.Clamp(speechVolume, 0, 1);
            synthesizer.Options.SpeakingRate = Math.Clamp(speechRate, 0.5, 6.0);
            synthesizer.Options.AudioPitch = Math.Clamp(speechPitch, 0.0, 2.0);
            synthesizer.Options.AppendedSilence = SpeechAppendedSilence.Min;
            synthesizer.Options.PunctuationSilence = SpeechPunctuationSilence.Min;
            return await synthesizer.SynthesizeTextToStreamAsync(content);
        });

        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            await mediaPlayer.PlayAsync(MediaSource.CreateFromStream(stream, stream.ContentType), ct);
            return true;
        });
    }
}

public class WindowsVoice : IVoice
{
    public VoiceInformation VoiceInfomation { get; }

    public WindowsVoice(VoiceInformation voiceInfomation)
    {
        VoiceInfomation = voiceInfomation;
    }

    public string Id => VoiceInfomation.Id;

    public string Name => VoiceInfomation.DisplayName;

    public string Language => VoiceInfomation.Language;

    public string Gender => VoiceInfomation.Gender.ToString();

    public override string ToString()
    {
        return $"{Name} ({Language})";
    }
}

