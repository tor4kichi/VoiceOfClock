using ABI.Windows.AI.MachineLearning;
using CommunityToolkit.Mvvm.DependencyInjection;
using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock.Views.Dialogs;

public sealed class PeriodicTimerEditDialogService : IPeriodicTimerDialogService
{
    public Task<PeriodicTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime, IEnumerable<DayOfWeek> enabledDayOfWeeks, DayOfWeek firstDayOfWeek)
    {
        var dialog = new PeriodicTimerEditDialog();
        App.Current.InitializeDialog(dialog);
        dialog.Title = dialogTitle;
        return dialog.ShowAsync(timerTitle, startTime, endTime, intervalTime, enabledDayOfWeeks, firstDayOfWeek);
    }
}

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
        TimePicker_IntervalTime.SelectedTime = intervalTime;

        var enabledDayOfWeeksHashSet = enabledDayOfWeeks.ToHashSet();
        EnabledDayOfWeeks = firstDayOfWeek.ToWeek()
            .Select(x => new EnabledDayOfWeekViewModel(x) { IsEnabled = enabledDayOfWeeksHashSet.Contains(x) })
            .ToArray();
        
        using var _ = EnabledDayOfWeeks.Select(x => x.ObserveProperty(x => x.IsEnabled)).CombineLatestValuesAreAllFalse()
            .Where(_ => !_isRepeatChanging)
            .Subscribe(x =>
            {
                IsRepeat = !x;
            });                

        if (await base.ShowAsync() is ContentDialogResult.Primary)
        {
            return new PeriodicTimerDialogResult
            {
                IsConfirmed = true,
                Title = TextBox_EditTitle.Text,
                StartTime = TimePicker_StartTime.SelectedTime ?? throw new InvalidOperationException(nameof(TimePicker_StartTime)),
                EndTime = TimePicker_EndTime.SelectedTime ?? throw new InvalidOperationException(nameof(TimePicker_EndTime)),
                IntervalTime = TimePicker_IntervalTime.SelectedTime ?? throw new InvalidOperationException(nameof(TimePicker_IntervalTime)),
                EnabledDayOfWeeks = EnabledDayOfWeeks.Where(x => x.IsEnabled).Select(x => x.DayOfWeek).ToArray(),
            };
        }
        else
        {
            return new PeriodicTimerDialogResult { IsConfirmed = false };
        }            
    }

    private void CheckBox_IsRepeat_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var newValue = IsRepeat;
        _isRepeatChanging = true;
        foreach (var item in EnabledDayOfWeeks)
        {
            item.IsEnabled = newValue;
        }

        _isRepeatChanging = false;
    }
}
