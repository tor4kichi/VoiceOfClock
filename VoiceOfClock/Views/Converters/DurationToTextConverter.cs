using I18NPortable;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Views.Converters;

public sealed class DurationToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeOnly timeOnly)
        {
            value = timeOnly.ToTimeSpan();
        }

        if (value is TimeSpan timeSpan)
        {
            return Convert(timeSpan);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    public static string Convert(TimeSpan timeSpan)
    {
        if (timeSpan.Days > 0)
        {
            return timeSpan.ToString("%d");
        }
        else if (timeSpan.Hours > 0)
        {
            return $"{timeSpan:h\\:mm\\:ss}";
        }
        else 
        {
            return $"{timeSpan:m\\:ss}";
        }        
    }
}
