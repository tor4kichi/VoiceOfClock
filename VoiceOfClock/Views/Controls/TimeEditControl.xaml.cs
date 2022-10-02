using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DependencyPropertyGenerator;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace VoiceOfClock.Views.Controls;


public enum TimeSpanPickerMode
{
    Hours_Minutes_Seconds,
    Hours_Minutes,
    Hours,
    Minutes_Seconds,
    Minutes,
    Seconds,
}

[DependencyProperty<TimeSpan>("Time")]
[DependencyProperty<TimeSpanPickerMode>("DisplayMode")]
public sealed partial class TimeEditControl : UserControl
{
    public TimeEditControl()
    {
        this.InitializeComponent();        
    }



    Brush _numberControlFocusBrush = new SolidColorBrush() { Color = Colors.Gray, Opacity = 0.5 };
    private void ContentControl_Hour_GotFocus(object sender, RoutedEventArgs e)
    {
        (sender as Control).Background = _numberControlFocusBrush;
    }

    private void ContentControl_Hour_LostFocus(object sender, RoutedEventArgs e)
    {
        (sender as Control).Background = null;
    }

    private void Button_Hour_Tapped(object sender, TappedRoutedEventArgs e)
    {
        (sender as Control).Background = _numberControlFocusBrush;
    }

    private void Button_Hour_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        Debug.WriteLine(e.Key);
    }
}
