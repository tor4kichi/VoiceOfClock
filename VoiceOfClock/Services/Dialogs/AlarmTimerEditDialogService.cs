﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Models;

namespace VoiceOfClock.Services.Dialogs;

public sealed class AlarmTimerEditDialogService : IAlarmTimerDialogService
{
    public Task<AlarmTimerDialogResult> ShowEditTimerAsync(
        string dialogTitle,
        string timerTitle,
        TimeOnly dayOfTime,
        TimeSpan? snooze,
        IEnumerable<DayOfWeek> enabledDayOfWeeks,
        DayOfWeek firstDayOfWeek,
        SoundSourceType soundSourceType,
        string soundContent,
        IEnumerable<TimeZoneInfo>? timeZones,
        int firstTimeZoneSelectedIndex
        )
    {
        var dialog = new Views.AlarmTimerEditDialog();
        App.Current.InitializeDialog(dialog);        
        return dialog.ShowAsync(
            dialogTitle,
            timerTitle,
            dayOfTime,
            snooze,
            enabledDayOfWeeks,
            firstDayOfWeek,
            soundSourceType,
            soundContent,            
            timeZones,
            firstTimeZoneSelectedIndex
            );
    }
}
