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
        public TimeSpan InstantPeriodicTimerInterval
        {
            get => Read(TimeSpan.FromMinutes(1));
            set => Save(value);
        }
    }
}
