using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.UseCases;

public static class TimeTranslationHelper
{
    public static string TranslateTimeSpan(this TimeSpan timeSpan)
    {
        bool hasHour = timeSpan.Hours >= 1;
        bool hasMinutes = timeSpan.Minutes >= 1;
        bool hasSeconds = timeSpan.Seconds >= 1;
        if (hasHour && hasMinutes && hasSeconds)
        {
            return "Time_HoursAndMinutesAndSeconds".Translate(timeSpan.Seconds, timeSpan.Minutes, timeSpan.Hours);
        }
        else if (hasHour && hasMinutes && !hasSeconds)
        {
            return "Time_HoursAndMinutes".Translate(timeSpan.Minutes, timeSpan.Hours);
        }
        else if (hasMinutes && hasSeconds && !hasHour)
        {
            return "Time_MinutesAndSeconds".Translate(timeSpan.Seconds, timeSpan.Minutes);
        }
        else if (hasHour && hasSeconds && !hasMinutes)
        {
            return "Time_HoursAndSeconds".Translate(timeSpan.Seconds, timeSpan.Hours);
        }
        else if (hasHour && !hasMinutes && !hasSeconds)
        {
            return "Time_Hours".Translate(timeSpan.Hours);
        }
        else if (hasMinutes && !hasHour && !hasSeconds)
        {
            return "Time_Minutes".Translate(timeSpan.Minutes);
        }
        else //if (hasSeconds && !hasHour && !hasMinutes)
        {
            return "Time_Seconds".Translate(timeSpan.Seconds);
        }
    }
    
    public static string Translate(this TimeOnly time)
    {
        return TranslateTimeSpan(time.ToTimeSpan());
    }
}
