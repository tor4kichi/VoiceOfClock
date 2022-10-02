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


        public string TranslateTimeOfDay(DateTime time)
        {
            TimeSpan timeOfDay = time.TimeOfDay;
            return "TimeOfDayToSpeechText".Translate(timeOfDay.Hours, timeOfDay.Minutes);
        }
    }
}
