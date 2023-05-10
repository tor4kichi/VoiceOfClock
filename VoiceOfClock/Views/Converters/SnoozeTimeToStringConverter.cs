using I18NPortable;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Views.Converters;

public sealed class SnoozeTimeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null)
        {
            return "AlarmTimer_SnoozeIgnore".Translate();
        }
        else if (value is TimeSpan snoozeTime)
        {
            if (snoozeTime == TimeSpan.Zero)
            {
                return "AlarmTimer_SnoozeIgnore".Translate();
            }
            else
            {
                return "AlarmTimer_SnoozeWithMinutes".Translate((int)snoozeTime.TotalMinutes);
            }
        }

        throw new NotSupportedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
