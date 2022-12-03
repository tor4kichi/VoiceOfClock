using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Views.Converters;

public sealed class TimeSpanToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan time)
        {
            if (targetType == typeof(double))
            {
                return time.TotalSeconds;
            }
            else if (targetType == typeof(string))
            {
                return time.TotalSeconds.ToString("F1");
            }
        }

        throw new NotSupportedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double d)
        {
            return TimeSpan.FromSeconds(d);
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}
