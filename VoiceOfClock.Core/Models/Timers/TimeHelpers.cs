using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Core.Models.Timers;

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
        var utcDateTime = now.Date + startTime;
        if (utcDateTime < now)
        {
            utcDateTime += TimeSpan.FromDays(1);
        }
        return CulcValidDateInEnabledWeekOfDays(utcDateTime, enabledDayOfWeeks);
    }

    private static DateTime CulcValidDateInEnabledWeekOfDays(DateTime canidateNextTime, IEnumerable<DayOfWeek> enabledDayOfWeeks)
    {
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

    public static DateTime CulcNextTimeWithTimeZone(DateTimeOffset utcNow, TimeSpan startTimeInTargetTZ, IEnumerable<DayOfWeek> enabledDayOfWeeks, TimeZoneInfo localTZ, TimeZoneInfo targetTZ)
    {
        // 現地時間において次回時刻と日付を割り出して       
        var now = TimeZoneInfo.ConvertTime(utcNow.DateTime, localTZ, targetTZ);
        var candidateNextTime = now.TimeOfDay < startTimeInTargetTZ
            ? now.Date + startTimeInTargetTZ
            : now.Date + startTimeInTargetTZ + TimeSpan.FromDays(1)
            ;       
        // 現地時間のまま有効な曜日である次の日を求めて
        DateTime targetTZNextTime = CulcValidDateInEnabledWeekOfDays(candidateNextTime, enabledDayOfWeeks);        
        // 現地時間からローカル時間に変換する
        return TimeZoneInfo.ConvertTime(targetTZNextTime, targetTZ, localTZ);
    }
}
