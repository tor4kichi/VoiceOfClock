using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Infrastructure;

namespace VoiceOfClock.Core.Models.Timers;

public sealed class TimerSettings : SettingsBase
{
    public TimerSettings()
    {
        _speechActorId = Read("", nameof(SpeechActorId));
        _speechRate = Read(1.0d, nameof(SpeechRate));
        _speechPitch = Read(1.0d, nameof(SpeechPitch));
        _speechVolume = Read(1.0d, nameof(SpeechVolume));
        _isTimeSpeechWith24h = Read(true, nameof(IsTimeSpeechWith24h));
        _useSsml = Read(true, nameof(UseSsml));
        _ampmPositionByLanguageCode = Read(_defaultAmpmPositionByLanguage, nameof(AmpmPositionByLanguageCode))!;

        _isMultiTimeZoneSupportEnabled = Read(false, nameof(IsMultiTimeZoneSupportEnabled));
        _additionalSupportTimeZoneIds = Read(new string[] { TimeZoneInfo.Local.Id }, nameof(AdditionalSupportTimeZoneIds))!;

        _instantPeriodicTimerInterval = Read(TimeSpan.FromMinutes(1), nameof(InstantPeriodicTimerInterval));
        _firstDayOfWeek = Read(CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek, nameof(FirstDayOfWeek));

        _instantOneShotTimerInterval = Read(TimeSpan.FromMinutes(3), nameof(InstantOneShotTimerInterval));
    }

    #region Timer Generic Settings

    private string? _speechActorId;
    public string? SpeechActorId
    {
        get => _speechActorId;
        set => SetProperty(ref _speechActorId, value);
    }



    public const double MinSpeechRate = 0.5d;
    public const double MaxSpeechRate = 4.0d;
    private double _speechRate;
    /// <summary>
    /// スピーチの話速設定（デフォルトは1.0）
    /// </summary>
    /// <example>0.5 ~ 4.0</example>
    public double SpeechRate
    {
        get => _speechRate;
        set => SetProperty(ref _speechRate, Math.Clamp(value, MinSpeechRate, MaxSpeechRate));
    }


    public const double MinSpeechPitch = 0.5d;
    public const double MaxSpeechPitch = 2.0d;
    private double _speechPitch;
    /// <summary>
    /// スピーチのピッチ（デフォルトは1.0）
    /// </summary>
    /// <example>+1.0Hz, -2Hz, </example>
    public double SpeechPitch
    {
        get => _speechPitch;
        set => SetProperty(ref _speechPitch, Math.Clamp(value, MinSpeechPitch, MaxSpeechPitch));
    }




    public const double MinSpeechVolume = 0.0d;
    public const double MaxSpeechVolume = 1.0d;
    private double _speechVolume;
    /// <summary>
    /// スピーチの音量（デフォルトは1.0）
    /// </summary>
    /// <remarks>Min 0.0 , Max 1.0</remarks>
    public double SpeechVolume
    {
        get => _speechVolume;
        set => SetProperty(ref _speechVolume, Math.Clamp(value, MinSpeechVolume, MaxSpeechVolume));
    }

    private bool _useSsml;

    /// <summary>
    /// trueの場合、24時間表記でスピーチさせる。<br /> falseの場合、AM/PM表記でスピーチさせる。
    /// </summary>
    public bool UseSsml
    {
        get => _useSsml;
        set => SetProperty(ref _useSsml, value);
    }


    private bool _isTimeSpeechWith24h;

    /// <summary>
    /// trueの場合、24時間表記でスピーチさせる。<br /> falseの場合、AM/PM表記でスピーチさせる。
    /// </summary>
    public bool IsTimeSpeechWith24h
    {
        get => _isTimeSpeechWith24h;
        set => SetProperty(ref _isTimeSpeechWith24h, value);
    }



    private readonly Dictionary<string, AMPMPosition> _defaultAmpmPositionByLanguage = new()
    {
        { "ja", AMPMPosition.Prefix },
        { "ko", AMPMPosition.Prefix },
        { "zh", AMPMPosition.Prefix },
    };

    private Dictionary<string, AMPMPosition> _ampmPositionByLanguageCode;

    public Dictionary<string, AMPMPosition> AmpmPositionByLanguageCode
    {
        get => _ampmPositionByLanguageCode;
        set => SetProperty(ref _ampmPositionByLanguageCode, value);
    }


    public AMPMPosition GetAmpmPosition(CultureInfo cultureInfo)
    {
        return GetAmpmPosition(cultureInfo.Name);
    }

    public AMPMPosition GetAmpmPosition(string languageCode)
    {
        if (_ampmPositionByLanguageCode.TryGetValue(languageCode, out var result))
        {
            return result;
        }

        if (languageCode.Contains('-'))
        {
            var countryCode = new string(languageCode.TakeWhile(c => c != '-').ToArray());
            _ampmPositionByLanguageCode.TryGetValue(countryCode, out result);

            return result;
        }
        else
        {
            return AMPMPosition.NoChange;
        }
    }

    public void SetAmpmPosition(string languageCode, AMPMPosition position)
    {
        _ampmPositionByLanguageCode.Remove(languageCode);
        _ampmPositionByLanguageCode.Add(languageCode, position);
        Save(AmpmPositionByLanguageCode, nameof(AmpmPositionByLanguageCode));
    }

    #endregion

    #region TimeZone Settings

    private bool _isMultiTimeZoneSupportEnabled;
    public bool IsMultiTimeZoneSupportEnabled
    {
        get => _isMultiTimeZoneSupportEnabled;
        set => SetProperty(ref _isMultiTimeZoneSupportEnabled, value);
    }

    private string[] _additionalSupportTimeZoneIds;
    public string[] AdditionalSupportTimeZoneIds
    {
        get => _additionalSupportTimeZoneIds;
        set => SetProperty(ref _additionalSupportTimeZoneIds, value);
    }

    #endregion TimeZone


    #region Periodic Timer Settings

    private TimeSpan _instantPeriodicTimerInterval;
    public TimeSpan InstantPeriodicTimerInterval
    {
        get => _instantPeriodicTimerInterval;
        set => SetProperty(ref _instantPeriodicTimerInterval, value);
    }


    private DayOfWeek _firstDayOfWeek;
    public DayOfWeek FirstDayOfWeek
    {
        get => _firstDayOfWeek;
        set => SetProperty(ref _firstDayOfWeek, value);
    }

    #endregion

    #region OneShot Timer Settings

    private TimeSpan _instantOneShotTimerInterval;
    public TimeSpan InstantOneShotTimerInterval
    {
        get => _instantOneShotTimerInterval;
        set => SetProperty(ref _instantOneShotTimerInterval, value);
    }
    #endregion
}

public enum AMPMPosition
{
    NoChange,
    Prefix,
    Postfix,
}