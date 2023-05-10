using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Core.Models.Timers;

public static class AlarmTimerConstants
{
    public static TimeSpan[] SnoozeTimes { get; } = new[]
        {
            5, 10, 20, 30, 60
        }
        .Select(x => TimeSpan.FromMinutes(x))
        .ToArray();
}
