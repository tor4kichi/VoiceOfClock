using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using I18NPortable;
using Microsoft.VisualBasic;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;

public sealed partial class AlarmTimerViewModel : ObservableObject
{
    public AlarmTimerViewModel(AlarmTimerRunningInfo runningInfo, DayOfWeek firstDayOfWeek, Action<AlarmTimerViewModel> onDeleteAction)
    {
        RunningInfo = runningInfo;
        _onDeleteAction = onDeleteAction;
        _title = RunningInfo.Title;
        _timeOfDay = RunningInfo.TimeOfDay;
        _soundSourceType = RunningInfo.SoundSourceType;
        _soundContent = RunningInfo.SoundContent;
        _snooze = RunningInfo.Snooze;

        EnabledDayOfWeeks = firstDayOfWeek.ToWeek()
            .Select(x => new EnabledDayOfWeekViewModel(x) { IsEnabled = runningInfo.EnabledDayOfWeeks.Contains(x) }).ToArray();

        IsEnabled = RunningInfo.ToReactivePropertyAsSynchronized(x => x.IsEnabled);
    }

    public AlarmTimerRunningInfo RunningInfo { get; }


    [ObservableProperty]
    private bool _isEditting;

    [ObservableProperty]
    private string _title;

    public IReactiveProperty<bool> IsEnabled { get; }

    [ObservableProperty]
    private TimeOnly _timeOfDay;

    public EnabledDayOfWeekViewModel[] EnabledDayOfWeeks { get; }

    [ObservableProperty]
    private TimeSpan? _snooze;

    [ObservableProperty]
    private SoundSourceType _soundSourceType;

    [ObservableProperty]
    private string _soundContent;
    private readonly Action<AlarmTimerViewModel> _onDeleteAction;

    public void RefrectValues()
    {
        Title = RunningInfo.Title;
        //IsEnabled.Value = RunningInfo.IsEnabled;
        TimeOfDay = RunningInfo.TimeOfDay;
        SoundSourceType = RunningInfo.SoundSourceType;
        SoundContent = RunningInfo.SoundContent;
        Snooze = RunningInfo.Snooze;
        foreach (var dayOfWeekVM in EnabledDayOfWeeks)
        {
            dayOfWeekVM.IsEnabled = RunningInfo.EnabledDayOfWeeks.Contains(dayOfWeekVM.DayOfWeek);
        }
    }


    public string LocalizeTime(TimeOnly timeSpan)
    {
        return "Time_Hours_Minutes".Translate(timeSpan.Minute, timeSpan.Hour);
    }

    public string LocalizeIntervalTime(TimeSpan? maybeTimeSpan)
    {
        if (maybeTimeSpan is null)
        {
            return string.Empty;
        }

        var timeSpan = maybeTimeSpan.Value;
        bool hasHour = timeSpan.Hours >= 1;
        bool hasMinutes = timeSpan.Minutes >= 1;
        bool hasSeconds = timeSpan.Seconds >= 1;
        if (hasHour && hasMinutes && hasSeconds)
        {
            return "IntervalTime_PerHoursAndMinutesAndSeconds".Translate(timeSpan.Seconds, timeSpan.Minutes, timeSpan.Hours);
        }
        else if (hasHour && hasMinutes && !hasSeconds)
        {
            return "IntervalTime_PerHoursAndMinutes".Translate(timeSpan.Minutes, timeSpan.Hours);
        }
        else if (hasMinutes && hasSeconds && !hasHour)
        {
            return "IntervalTime_PerMinutesAndSeconds".Translate(timeSpan.Seconds, timeSpan.Minutes);
        }
        else if (hasHour && hasSeconds && !hasMinutes)
        {
            return "IntervalTime_PerHoursAndSeconds".Translate(timeSpan.Seconds, timeSpan.Hours);
        }
        else if (hasHour && !hasMinutes && !hasSeconds)
        {
            return "IntervalTime_PerHours".Translate(timeSpan.Hours);
        }
        else if (hasMinutes && !hasHour && !hasSeconds)
        {
            return "IntervalTime_PerMinutes".Translate(timeSpan.Minutes);
        }
        else if (hasSeconds && !hasHour && !hasMinutes)
        {
            return "IntervalTime_PerSeconds".Translate(timeSpan.Seconds);
        }
        else
        {
            return "IntervalTime_PerSeconds".Translate(timeSpan.Seconds);
        }
    }

    [RelayCommand]
    public void Delete()
    {
        _onDeleteAction(this);
    }
}
