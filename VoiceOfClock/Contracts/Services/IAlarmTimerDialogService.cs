using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceOfClock.Core.Domain;

namespace VoiceOfClock.Contracts.Services;

public interface IAlarmTimerDialogService
{
    Task<AlarmTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle, TimeOnly dayOfTime, TimeSpan? snooze, IEnumerable<DayOfWeek> enabledDayOfWeeks, DayOfWeek firstDayOfWeek, SoundSourceType soundSourceType, string soundContent);
}

public sealed class AlarmTimerDialogResult
{
    public bool IsConfirmed { get; init; }
    public string Title { get; init; } = String.Empty;
    public TimeOnly TimeOfDay { get; init; }
    public TimeSpan? Snooze { get; init; }
    public DayOfWeek[] EnabledDayOfWeeks { get; init; } = Array.Empty<DayOfWeek>();
    public SoundSourceType SoundSourceType { get; init; }
    public string SoundContent { get; init; } = string.Empty;
}