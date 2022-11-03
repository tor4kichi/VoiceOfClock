using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;
using System.Reactive.Linq;
using VoiceOfClock.Models.Domain;

namespace VoiceOfClock.UseCases;

[ObservableObject]
public sealed partial class PeriodicTimerRunningInfo : DeferUpdatable, IRunningTimer
{
    public PeriodicTimerRunningInfo(PeriodicTimerEntity entity, PeriodicTimerRepository repository)
    {
        _entity = entity;
        _repository = repository;
        _isEnabled = entity.IsEnabled;
        _startTime = entity.StartTime;
        _endTime = entity.EndTime;
        _intervalTime = entity.IntervalTime;
        _title = entity.Title;
        _enabledDayOfWeeks = entity.EnabledDayOfWeeks;

        CalcNextTime();
    }

    protected override void OnDeferUpdate()
    {
        CalcNextTime();
        Save();
    }

    public bool IsInstantTimer => _entity.Id == Guid.Empty;

    internal PeriodicTimerEntity _entity;

    private readonly PeriodicTimerRepository _repository;

    [ObservableProperty]
    private DateTime _nextTime;

    [ObservableProperty]
    private DateTime _startDateTime;

    [ObservableProperty]
    private TimeSpan _elapsedTime;


    [ObservableProperty]
    private bool _isInsidePeriod;

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        _entity.IsEnabled = value;
        if (!NowDeferUpdateRequested && !IsInstantTimer)
        {
            _repository.UpdateItem(_entity);
        }
    }

    [ObservableProperty]
    private TimeSpan _intervalTime;

    partial void OnIntervalTimeChanged(TimeSpan value)
    {
        _entity.IntervalTime = value;
        if (!NowDeferUpdateRequested && !IsInstantTimer)
        {
            _repository.UpdateItem(_entity);
            CalcNextTime();
        }
    }

    [ObservableProperty]
    private TimeSpan _startTime;

    partial void OnStartTimeChanged(TimeSpan value)
    {
        _entity.StartTime = value;
        if (!NowDeferUpdateRequested && !IsInstantTimer)
        {
            _repository.UpdateItem(_entity);
            CalcNextTime();
        }
    }

    [ObservableProperty]
    private TimeSpan _endTime;

    partial void OnEndTimeChanged(TimeSpan value)
    {
        _entity.EndTime = value;
        if (!NowDeferUpdateRequested && !IsInstantTimer)
        {
            _repository.UpdateItem(_entity);
            CalcNextTime();
        }
    }

    [ObservableProperty]
    private string _title;

    partial void OnTitleChanged(string value)
    {
        _entity.Title = value;
        if (!NowDeferUpdateRequested && !IsInstantTimer)
        {
            _repository.UpdateItem(_entity);
        }
    }

    [ObservableProperty]
    private DayOfWeek[] _enabledDayOfWeeks;

    partial void OnEnabledDayOfWeeksChanged(DayOfWeek[] value)
    {
        _entity.EnabledDayOfWeeks = value;
        if (!NowDeferUpdateRequested && !IsInstantTimer)
        {
            _repository.UpdateItem(_entity);
            CalcNextTime();
        }
    }

    public void UpdateEntity(PeriodicTimerEntity entity)
    {
        _entity.Title = entity.Title;
        _entity.StartTime = entity.StartTime;
        _entity.EndTime = entity.EndTime;
        _entity.IntervalTime = entity.IntervalTime;
        _entity.IsEnabled = entity.IsEnabled;
        _entity.EnabledDayOfWeeks = entity.EnabledDayOfWeeks.ToArray();
        if (!NowDeferUpdateRequested && !IsInstantTimer)
        {
            _repository.UpdateItem(_entity);
        }
        CalcNextTime();
    }

    void Save()
    {
        if (!NowDeferUpdateRequested && !IsInstantTimer)
        {
            _repository.UpdateItem(_entity);
        }
    }

    void CalcNextTime()
    {
        DateTime now = DateTime.Now;             
        IsInsidePeriod = TimeHelpers.IsInsideTime(now.TimeOfDay, _entity.StartTime, _entity.EndTime);
        if (IsInsidePeriod)
        {
            if (_entity.StartTime > now.TimeOfDay)
            {
                StartDateTime = DateTime.Today + _entity.StartTime - TimeSpan.FromDays(1);
            }
            else
            {
                StartDateTime = DateTime.Today + _entity.StartTime;
            }

            ElapsedTime = (now - StartDateTime).TrimMilliSeconds();
            int count = (int)Math.Ceiling(ElapsedTime / _entity.IntervalTime);
            NextTime = DateTime.Today + _entity.StartTime + _entity.IntervalTime * count;
        }
        else
        {
            NextTime = TimeHelpers.CulcNextTime(DateTime.Now, _entity.StartTime, EnabledDayOfWeeks);                
        }
    }

    public void OnEnded()
    {
        if (EnabledDayOfWeeks.Any() is false)
        {
            IsEnabled = false;
        }

        CalcNextTime();
    }

    public void IncrementNextTime()
    {
        NextTime += _entity.IntervalTime;
    }

    public void UpdateElapsedTime()
    {
        if (IsInsidePeriod = TimeHelpers.IsInsideTime(DateTime.Now.TimeOfDay, _entity.StartTime, _entity.EndTime))
        {                
            ElapsedTime = (DateTime.Now - StartDateTime).TrimMilliSeconds();
        }
        else
        {
            ElapsedTime = TimeSpan.Zero;
        }
    }
}
