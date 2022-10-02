using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VoiceOfClock.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock.Views.Dialogs
{
    public sealed class PeriodicTimerEditDialogService : IPeriodicTimerDialogService
    {
        public Task<PeriodicTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime)
        {
            var dialog = new PeriodicTimerEditDialog();
            App.Current.InitializeDialog(dialog);
            dialog.Title = dialogTitle;
            return dialog.ShowAsync(timerTitle, startTime, endTime, intervalTime);
        }
    }

    public sealed partial class PeriodicTimerEditDialog : ContentDialog
    {
        public PeriodicTimerEditDialog()
        {
            this.InitializeComponent();
        }

        public async Task<PeriodicTimerDialogResult> ShowAsync(string timerTitle, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime)
        {
            TextBox_EditTitle.Text = timerTitle;
            TimePicker_StartTime.SelectedTime = startTime;
            TimePicker_EndTime.SelectedTime = endTime;
            TimePicker_IntervalTime.SelectedTime = intervalTime;

            if (await base.ShowAsync() is ContentDialogResult.Primary)
            {
                return new PeriodicTimerDialogResult
                {
                    IsConfirmed = true,
                    Title = TextBox_EditTitle.Text,
                    StartTime = TimePicker_StartTime.SelectedTime ?? throw new InvalidOperationException(nameof(TimePicker_StartTime)),
                    EndTime = TimePicker_EndTime.SelectedTime ?? throw new InvalidOperationException(nameof(TimePicker_EndTime)),
                    IntervalTime = TimePicker_IntervalTime.SelectedTime ?? throw new InvalidOperationException(nameof(TimePicker_IntervalTime)),
                };
            }
            else
            {
                return new PeriodicTimerDialogResult { IsConfirmed = false };
            }            
        }
    }
}
