using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Infrastructure;

namespace VoiceOfClock.Models.Domain
{
    
    public sealed class TimerSettings : SettingsBase
    {
        public TimerSettings()
        {
            _instantPeriodicTimerInterval = Read(TimeSpan.FromMinutes(1), nameof(InstantPeriodicTimerInterval));
            _speechActorId = Read("", nameof(SpeechActorId));
            _speechRate = Read(1.0d, nameof(SpeechRate));
            _speechPitch = Read(1.0d, nameof(SpeechPitch));
        }

        private TimeSpan _instantPeriodicTimerInterval;
        public TimeSpan InstantPeriodicTimerInterval
        {
            get => _instantPeriodicTimerInterval;
            set => SetProperty(ref _instantPeriodicTimerInterval, value);
        }



        private string _speechActorId;
        public string SpeechActorId
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
        /// <see cref="https://www.asahi-net.or.jp/~ax2s-kmtn/ref/accessibility/REC-speech-synthesis11-20100907.html#edef_prosody"/>
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
        /// <see cref="https://www.asahi-net.or.jp/~ax2s-kmtn/ref/accessibility/REC-speech-synthesis11-20100907.html#edef_prosody"/>
        public double SpeechPitch
        {
            get => _speechPitch;
            set => SetProperty(ref _speechPitch, Math.Clamp(value, MinSpeechPitch, MaxSpeechPitch));
        }
    }
}

