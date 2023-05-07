#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace VoiceOfClock.Views;

public sealed partial class SettingsTimeZoneSelectDialog : ContentDialog
{
    public SettingsTimeZoneSelectDialog(IEnumerable<TimeZoneInfo> timeZoneInfos)
    {
        this.InitializeComponent();

        TimeZonesListView.ItemsSource = timeZoneInfos.ToArray();
    }

    public TimeZoneInfo? SelectedTimeZoneInfo
    {
        get => TimeZonesListView.SelectedItem as TimeZoneInfo;
        set => TimeZonesListView.SelectedItem = value;
    }
}
