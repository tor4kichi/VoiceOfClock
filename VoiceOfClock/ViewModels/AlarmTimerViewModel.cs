using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using I18NPortable;
using Microsoft.UI.Dispatching;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Windows.Input;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.ViewModels;

[ObservableObject]
public sealed partial class AlarmTimerViewModel
{
    public AlarmTimerEntity Entity { get; }
    private readonly AlarmTimerLifetimeManager _alarmTimerLifetimeManager;
    public ICommand DeleteCommand { get; }  

    public AlarmTimerViewModel(AlarmTimerEntity entity, DayOfWeek firstDayOfWeek, AlarmTimerLifetimeManager alarmTimerLifetimeManager, ICommand deleteCommand)
    {
        Entity = entity;
        _alarmTimerLifetimeManager = alarmTimerLifetimeManager;
        DeleteCommand = deleteCommand;
        _title = Entity.Title;
        _timeOfDay = Entity.TimeOfDay;
        _soundSourceType = Entity.SoundSourceType;
        _soundContent = Entity.SoundContent;
        _snooze = Entity.Snooze;
        _isEnabled = entity.IsEnabled;

        EnabledDayOfWeeks = firstDayOfWeek.ToWeek()
            .Select(x => new EnabledDayOfWeekViewModel(x) { IsEnabled = entity.EnabledDayOfWeeks.Contains(x) }).ToArray();

        CulcTargetTime();
    }

    public Guid EntityId => Entity.Id;

    private void Save()
    {
        _alarmTimerLifetimeManager.UpdateAlarmTimer(Entity);
    }


    [ObservableProperty]
    private bool _isEditting;

    [ObservableProperty]
    private string _title;


    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        Entity.IsEnabled = value;
        Save();        
    }

    [ObservableProperty]
    private TimeOnly _timeOfDay;

    public EnabledDayOfWeekViewModel[] EnabledDayOfWeeks { get; }

    [ObservableProperty]
    private TimeSpan? _snooze;

    [ObservableProperty]
    private SoundSourceType _soundSourceType;

    [ObservableProperty]
    private string _soundContent;

    [ObservableProperty]
    private DateTime _targetTime;

    public void CulcTargetTime()
    {
        TargetTime = TimeHelpers.CulcNextTime(DateTime.Now, Entity.TimeOfDay.ToTimeSpan(), Entity.EnabledDayOfWeeks);
    }

    public void RefrectValues()
    {
        Title = Entity.Title;
        IsEnabled = Entity.IsEnabled;
        TimeOfDay = Entity.TimeOfDay;
        SoundSourceType = Entity.SoundSourceType;
        SoundContent = Entity.SoundContent;
        Snooze = Entity.Snooze;
        foreach (var dayOfWeekVM in EnabledDayOfWeeks)
        {
            dayOfWeekVM.IsEnabled = Entity.EnabledDayOfWeeks.Contains(dayOfWeekVM.DayOfWeek);
        }

        CulcTargetTime();
    }


    public void RefrectBackValues()
    {
        Entity.Title = Title;
        Entity.IsEnabled = IsEnabled;
        Entity.TimeOfDay = TimeOfDay;
        Entity.SoundSourceType = SoundSourceType;
        Entity.SoundContent = SoundContent;
        Entity.Snooze = Snooze;
        Entity.EnabledDayOfWeeks = EnabledDayOfWeeks.Where(x => x.IsEnabled).Select(x => x.DayOfWeek).ToArray();

        Save();
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
}
