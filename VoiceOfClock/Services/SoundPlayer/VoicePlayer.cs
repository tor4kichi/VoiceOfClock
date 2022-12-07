using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using DryIoc;
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
using System.Windows.Forms;
using System.Windows.Markup;
using VoiceOfClock.Contract.Services;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;
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

    public Task<bool> PlayTimeOfDayAsync(MediaPlayer mediaPlayer, DateTime time, CancellationToken ct)
    {
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

            // SSML使用時はレートとピッチの指定をSSML内（SsmlHelpers.ToSsml1_0Formatなど）で指定しているのでPlay側では指定をスキップしている
            if (currentVoicePlayer is SystemVoicePlayer)
            {
                string speechData = SsmlHelpers.ToSsml1_0Format(_translationProcesser.Translate("TimeOfDayToSpeechText", ToSsmlTimeFormat_HM(time, _timerSettings.IsTimeSpeechWith24h, cutureInfo.DateTimeFormat, _timerSettings.GetAmpmPosition(cutureInfo))), _timerSettings.SpeechRate, _timerSettings.SpeechPitch, voiceLanguage);
                return currentVoicePlayer.PlayVoiceWithSsmlAsync(mediaPlayer, speechData, speechVolume: _timerSettings.SpeechVolume, ct: ct);
            }
            else if (currentVoicePlayer is WindowsVoicePlayer)
            {
                string speechData = SsmlHelpers.ToSsml1_1Format(_translationProcesser.Translate("TimeOfDayToSpeechText", ToSsmlTimeFormat_HM(time, _timerSettings.IsTimeSpeechWith24h, cutureInfo.DateTimeFormat, _timerSettings.GetAmpmPosition(cutureInfo))), _timerSettings.SpeechRate, _timerSettings.SpeechPitch, voiceLanguage);
                return currentVoicePlayer.PlayVoiceWithSsmlAsync(mediaPlayer, speechData, speechVolume: _timerSettings.SpeechVolume, ct: ct);
            }
            else
            {
                throw new NotSupportedException("VoicePlayer not found.");
            }
        }
        else
        {
            string speechData = _translationProcesser.Translate("TimeOfDayToSpeechText", _translationProcesser.TranslateTimeOfDay(time, _timerSettings.IsTimeSpeechWith24h));
            return currentVoicePlayer.PlayVoiceWithTextAsync(mediaPlayer, speechData, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume, ct);
        }
    }


    public Task<bool> PlayTextAsync(MediaPlayer mediaPlayer, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(false);
        }

        string? voiceId = _timerSettings.SpeechActorId;
        var currentVoicePlayer = _supportedVoicePlayers.FirstOrDefault(x => x.CanPlayVoice(voiceId));
        if (currentVoicePlayer is null)
        {
            _timerSettings.SpeechActorId = "";
            currentVoicePlayer = FallbackVoicePlayer;
            voiceId = null;
        }
        
        return currentVoicePlayer.PlayVoiceWithTextAsync(mediaPlayer, text, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume, cancellationToken);
    }

    public Task<bool> PlayTextWithSsmlAsync(MediaPlayer mediaPlayer, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(false);
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

        return currentVoicePlayer.PlayVoiceWithSsmlAsync(mediaPlayer, text, _timerSettings.SpeechRate, _timerSettings.SpeechPitch, _timerSettings.SpeechVolume, cancellationToken);
    }


}

public interface IVoicePlayer
{
    IReadOnlyCollection<string> GetAllVoiceId();
    bool CanPlayVoice(string? voiceId);
    string SetVoice(string? voiceId);
    Task<bool> PlayVoiceWithSsmlAsync(MediaPlayer mediaPlayer, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default);
    Task<bool> PlayVoiceWithTextAsync(MediaPlayer mediaPlayer, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default);
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

    public async Task<bool> PlayVoiceWithTextAsync(MediaPlayer mediaPlayer, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default)
    {
        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var speakCompletedObservable = Observable.FromEventPattern<SpeakCompletedEventArgs>(
                h => _speechSynthesiser.SpeakCompleted += h,
                h => _speechSynthesiser.SpeakCompleted -= h
                ).Take(1);

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



    public async Task<bool> PlayVoiceWithSsmlAsync(MediaPlayer mediaPlayer, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default)
    {
        return await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var speakCompletedObservable = Observable.FromEventPattern<SpeakCompletedEventArgs>(
                h => _speechSynthesiser.SpeakCompleted += h,
                h => _speechSynthesiser.SpeakCompleted -= h
                ).Take(1);
            
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



public sealed class WindowsVoicePlayer : IVoicePlayer
{
    private readonly DispatcherQueue _dispatcherQueue;

    public WindowsVoicePlayer()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

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

    public async Task<bool> PlayVoiceWithSsmlAsync(MediaPlayer mediaPlayer, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default)
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
            await mediaPlayer.PlayAsync(MediaSource.CreateFromStream(stream, stream.ContentType), ct);
            return true;
        });
    }


    public async Task<bool> PlayVoiceWithTextAsync(MediaPlayer mediaPlayer, string content, double speechRate = 1, double speechPitch = 1, double speechVolume = 1, CancellationToken ct = default)
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
            await mediaPlayer.PlayAsync(MediaSource.CreateFromStream(stream, stream.ContentType), ct);
            return true;
        });
    }
}
