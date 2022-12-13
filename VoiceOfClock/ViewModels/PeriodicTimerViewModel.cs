﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;

public partial class PeriodicTimerViewModel : ObservableObject
{
    public PeriodicTimerEntity Entity { get; }
    private readonly PeriodicTimerLifetimeManager _periodicTimerLifetimeManager;
    public ICommand DeleteCommand { get; }

    public PeriodicTimerViewModel(PeriodicTimerEntity entity, PeriodicTimerLifetimeManager periodicTimerLifetimeManager, ICommand deleteCommand, DayOfWeek firstDayOfWeek)
    {
        Entity = entity;
        _periodicTimerLifetimeManager = periodicTimerLifetimeManager;
        DeleteCommand = deleteCommand;

        _isEnabled = Entity.IsEnabled;
        _intervalTime = Entity.IntervalTime;
        _startTime = Entity.StartTime;
        _endTime = Entity.EndTime;
        _title = Entity.Title;
        
        EnabledDayOfWeeks = firstDayOfWeek.ToWeek()
            .Select(x => new EnabledDayOfWeekViewModel(x) { IsEnabled = entity.EnabledDayOfWeeks.Contains(x) }).ToArray();

        CulcNextTime();
    }

    public void RefrectValues()
    {
        IsEnabled = Entity.IsEnabled;
        IntervalTime = Entity.IntervalTime;
        StartTime = Entity.StartTime;
        EndTime = Entity.EndTime;
        Title = Entity.Title;        
        foreach (var dayOfWeekVM in EnabledDayOfWeeks)
        {
            dayOfWeekVM.IsEnabled = Entity.EnabledDayOfWeeks.Contains(dayOfWeekVM.DayOfWeek);
        }
    }

    public bool IsRemovable => !_periodicTimerLifetimeManager.IsInstantPeriodicTimer(Entity);

    [ObservableProperty]
    private bool _isEditting;

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {        
        if (!IsRemovable) { return; }
        Entity.IsEnabled = value;
    }


    [ObservableProperty]
    private TimeSpan _intervalTime;

    [ObservableProperty]
    private TimeSpan _startTime;

    [ObservableProperty]
    private TimeSpan _endTime;

    [ObservableProperty]
    private string _title;


    public EnabledDayOfWeekViewModel[] EnabledDayOfWeeks { get; }

    [ObservableProperty]
    private bool _isInsidePeriod;


    [ObservableProperty]
    private DateTime _nextTime;

    [ObservableProperty]
    private DateTime _startDateTime;

    [ObservableProperty]
    private TimeSpan _elapsedTime;

    public void CulcNextTime()
    {        
        if (IsInsidePeriod = PeriodicTimerLifetimeManager.TimerIsInsidePeriod(Entity))
        {
            (StartDateTime, ElapsedTime, NextTime) = PeriodicTimerLifetimeManager.InsidePeriodCulcNextTime(Entity);
        }
        else
        {
            NextTime = PeriodicTimerLifetimeManager.OutsideCulcNextTime(Entity);
            ElapsedTime = TimeSpan.Zero;
        }
    }

    public void UpdateElapsedTime()
    {        
        if (IsInsidePeriod = PeriodicTimerLifetimeManager.TimerIsInsidePeriod(Entity))
        {
            ElapsedTime = (DateTime.Now - StartDateTime).TrimMilliSeconds();
        }
        else
        {
            ElapsedTime = TimeSpan.Zero;
        }
    }


    public string LocalizeTime(TimeSpan timeSpan)
    {
        return "Time_Hours_Minutes".Translate(timeSpan.Minutes, timeSpan.Hours);
    }

    public string LocalizeIntervalTime(TimeSpan timeSpan)
    {
        return "IntervalTime_PerVariableTime".Translate(TimeTranslationHelper.TranslateTimeSpan(timeSpan));
    }
}
