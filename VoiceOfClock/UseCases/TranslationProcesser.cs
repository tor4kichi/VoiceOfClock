using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.UseCases
{
    public sealed class TranslationProcesser 
    {
        public TranslationProcesser()
        {
        }


        public string TranslateTimeOfDay(DateTime time, bool is24h)
        {
            TimeSpan timeOfDay = time.TimeOfDay;
            if (is24h)
            {
                return "TimeOfDayToSpeechText_Hour_Minute".Translate(timeOfDay.Hours, timeOfDay.Minutes);
            }
            else
            {                
                if (timeOfDay.Hours < 12)
                {
                    return "TimeOfDayToSpeechText_AMPM_Hour_Minute".Translate("Clock_AM".Translate(), timeOfDay.Hours, timeOfDay.Minutes);
                }
                else
                {
                    return "TimeOfDayToSpeechText_AMPM_Hour_Minute".Translate("Clock_PM".Translate(), timeOfDay.Hours - 12, timeOfDay.Minutes);
                }
                
            }
        }
    }
}
