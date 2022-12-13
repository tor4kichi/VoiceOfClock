using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;

namespace VoiceOfClock.Services.Dialogs;

public sealed class PeriodicTimerEditDialogService : IPeriodicTimerDialogService
{
    public Task<PeriodicTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime, IEnumerable<DayOfWeek> enabledDayOfWeeks, DayOfWeek firstDayOfWeek)
    {
        var dialog = new Views.PeriodicTimerEditDialog();
        App.Current.InitializeDialog(dialog);
        dialog.Title = dialogTitle;
        return dialog.ShowAsync(timerTitle, startTime, endTime, intervalTime, enabledDayOfWeeks, firstDayOfWeek);
    }
}
