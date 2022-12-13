using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Core.Domain;

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
            return startTimeOfDay <= targetTimeOfDay && targetTimeOfDay <= endTimeOfDay;
        }
        else
        {
            return !(endTimeOfDay <= targetTimeOfDay && targetTimeOfDay <= startTimeOfDay);
        }        
    }

    public static TimeSpan TrimMilliSeconds(this TimeSpan ts)
    {        
        return new TimeSpan(ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
    }

    public static IEnumerable<DayOfWeek> ToWeek(this DayOfWeek firstDayOfWeek)
    {        
        foreach (var i in Enumerable.Range((int)firstDayOfWeek, 7))
        {
            int index = i;
            if (index >= 7)
            {
                index = index - 7;
            }

            yield return (DayOfWeek)index;
        }
    }

    public static DateTime CulcNextTime(DateTime now, TimeSpan startTime, IEnumerable<DayOfWeek> enabledDayOfWeeks)
    {
        DateTime canidateNextTime;
        if (startTime > now.TimeOfDay)
        {
            canidateNextTime = now.Date + startTime;
        }
        else
        {
            canidateNextTime = now.Date + startTime + TimeSpan.FromDays(1);
        }

        if (enabledDayOfWeeks.Any() is false || enabledDayOfWeeks.Contains(canidateNextTime.DayOfWeek))
        {
            return canidateNextTime;
        }
        else if (enabledDayOfWeeks.Any())
        {
            foreach (var i in Enumerable.Range(0, 7))
            {
                canidateNextTime += TimeSpan.FromDays(1);
                if (enabledDayOfWeeks.Contains(canidateNextTime.DayOfWeek))
                {
                    return canidateNextTime;
                }
            }
        }        

        throw new NotSupportedException();
    }
}
