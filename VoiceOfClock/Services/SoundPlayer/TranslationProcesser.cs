using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Domain;

namespace VoiceOfClock.UseCases
{
    public sealed class TranslationProcesser 
    {
        private readonly II18N _localize;

        public TranslationProcesser()
        {            
            _localize = new I18N().Init(typeof(App).Assembly)
                .SetFallbackLocale("en-US")
                .SetNotFoundSymbol("🍣")
#if DEBUG
                .SetThrowWhenKeyNotFound(true)
#endif
                ;
        }

        public void SetLocale(string language)
        {
            _localize.Locale = language;
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

        public (string TimeText, string AMPMText) TranslateAMPM(DateTime dateTime)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(_localize.Locale);
            return (dateTime.ToString("h:m"), dateTime.ToString("tt", cultureInfo.DateTimeFormat));
        }

        public string TranslateAMPMWithNormalFormat(DateTime dateTime)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(_localize.Locale);
            return dateTime.ToString("t", cultureInfo.DateTimeFormat);
        }

        public string Translate(string key)
        {
            return _localize.Translate(key);
        }

        public string Translate(string key, params object[] args)
        {
            return _localize.Translate(key, args);
        }        
    }
}
