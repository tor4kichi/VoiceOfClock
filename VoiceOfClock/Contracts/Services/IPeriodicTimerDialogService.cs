using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VoiceOfClock.Contracts.Services;

public interface IPeriodicTimerDialogService
{
    Task<PeriodicTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime, IEnumerable<DayOfWeek> enabledDayOfWeeks, DayOfWeek firstDayOfWeek);
}

public sealed class PeriodicTimerDialogResult
{
    public bool IsConfirmed { get; init; }
    public string Title { get; init; } = string.Empty;
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public TimeSpan IntervalTime { get; init; }
    public DayOfWeek[] EnabledDayOfWeeks { get; init; } = Array.Empty<DayOfWeek>();
}