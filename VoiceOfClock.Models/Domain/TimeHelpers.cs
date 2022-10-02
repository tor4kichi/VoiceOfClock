using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Models.Domain;

public static class TimeHelpers
{
    public static bool IsInsideTime(this TimeSpan targetTimeOfDay, TimeSpan startTimeOfDay, TimeSpan endTimeOfDay)
    {
        if (targetTimeOfDay > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(targetTimeOfDay), "targetTime must equal less than 24 hours.");
        }

        if (startTimeOfDay > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(startTimeOfDay), "startTime must equal less than 24 hours.");
        }

        if (endTimeOfDay > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(endTimeOfDay), "endTime must equal less than 24 hours.");
        }

        if (startTimeOfDay < endTimeOfDay)
        {
            return startTimeOfDay < targetTimeOfDay && targetTimeOfDay < endTimeOfDay;
        }
        else
        {
            return !(endTimeOfDay < targetTimeOfDay && targetTimeOfDay < startTimeOfDay);
        }
    }
}
