using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Views.Converters;

public sealed class SecondsToTimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
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

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan time)
        {
            return time.TotalSeconds;
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}
