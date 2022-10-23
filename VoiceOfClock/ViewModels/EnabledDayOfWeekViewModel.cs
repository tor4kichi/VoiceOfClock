using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace VoiceOfClock.ViewModels;

public sealed partial class EnabledDayOfWeekViewModel : ObservableObject
{
    public EnabledDayOfWeekViewModel(DayOfWeek dayOfWeek)
    {
        DayOfWeek = dayOfWeek;
    }

    public DayOfWeek DayOfWeek { get; }


    [ObservableProperty]
    private bool _isEnabled;
}