﻿using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Models.Timers;
using VoiceOfClock.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace VoiceOfClock.Views;

[DependencyProperty<bool>("IsRepeat")]
[DependencyProperty<EnabledDayOfWeekViewModel[]>("EnabledDayOfWeeks")]
public sealed partial class PeriodicTimerEditDialog : ContentDialog
{
    public PeriodicTimerEditDialog()
    {
        this.InitializeComponent();
    }

    bool _isRepeatChanging = false;


    public async Task<PeriodicTimerDialogResult> ShowAsync(string timerTitle, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime, IEnumerable<DayOfWeek> enabledDayOfWeeks, DayOfWeek firstDayOfWeek)
    {
        TextBox_EditTitle.Text = timerTitle;
        TimePicker_StartTime.SelectedTime = startTime;
        TimePicker_EndTime.SelectedTime = endTime;
        TimeSelectBox_IntervalTime.Time = intervalTime;

        var enabledDayOfWeeksHashSet = enabledDayOfWeeks.ToHashSet();
        EnabledDayOfWeeks = firstDayOfWeek.ToWeek()
            .Select(x => new EnabledDayOfWeekViewModel(x) { IsEnabled = enabledDayOfWeeksHashSet.Contains(x) })
            .ToArray();

        TimePicker_StartTime.SelectedTimeChanged += TimePicker_StartTime_SelectedTimeChanged;
        TimePicker_EndTime.SelectedTimeChanged += TimePicker_StartTime_SelectedTimeChanged;
        TimeSelectBox_IntervalTime.TimeChanged += TimeSelectBox_IntervalTime_TimeChanged; ;

        var disposer = EnabledDayOfWeeks.Select(x => x.ObserveProperty(x => x.IsEnabled)).CombineLatestValuesAreAllFalse()
            .Where(_ => !_isRepeatChanging)
            .Subscribe(x =>
            {
                IsRepeat = !x;
            });

        try
        {
            if (await base.ShowAsync() is ContentDialogResult.Primary)
            {
                return new PeriodicTimerDialogResult
                {
                    IsConfirmed = true,
                    Title = TextBox_EditTitle.Text,
                    StartTime = TimePicker_StartTime.SelectedTime ?? throw new InvalidOperationException(nameof(TimePicker_StartTime)),
                    EndTime = TimePicker_EndTime.SelectedTime ?? throw new InvalidOperationException(nameof(TimePicker_EndTime)),
                    IntervalTime = TimeSelectBox_IntervalTime.Time,
                    EnabledDayOfWeeks = EnabledDayOfWeeks.Where(x => x.IsEnabled).Select(x => x.DayOfWeek).ToArray(),
                };
            }
            else
            {
                return new PeriodicTimerDialogResult { IsConfirmed = false };
            }
        }
        finally
        {
            disposer.Dispose();
            TimePicker_StartTime.SelectedTimeChanged -= TimePicker_StartTime_SelectedTimeChanged;
            TimePicker_EndTime.SelectedTimeChanged -= TimePicker_StartTime_SelectedTimeChanged;
            TimeSelectBox_IntervalTime.TimeChanged -= TimeSelectBox_IntervalTime_TimeChanged; ;
        }        
    }


    private void CheckBox_IsRepeat_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var newValue = IsRepeat;
        _isRepeatChanging = true;
        foreach (var item in EnabledDayOfWeeks!)
        {
            item.IsEnabled = newValue;
        }

        _isRepeatChanging = false;
    }

    private void TimePicker_StartTime_SelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
    {
        UpdateIsPrimaryButtonEnabled();
    }


    private void TimeSelectBox_IntervalTime_TimeChanged(Controls.TimeSelectBox sender, Controls.TimeSelectBoxTimeValueChangedEventArgs args)
    {
        UpdateIsPrimaryButtonEnabled();
    }

    private void UpdateIsPrimaryButtonEnabled()
    {        
        if (TimePicker_StartTime.SelectedTime == TimePicker_EndTime.SelectedTime
            || TimeSelectBox_IntervalTime.Time == TimeSpan.Zero
            )
        {
            IsPrimaryButtonEnabled = false;
        }
        else
        {
            IsPrimaryButtonEnabled = true;
        }
    }
}
