using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Linq;
using VoiceOfClock.Models.Domain;

namespace VoiceOfClock.UseCases;

[ObservableObject]
public sealed partial class AlarmTimerRunningInfo : DeferUpdatable, IRunningTimer
{
    public AlarmTimerRunningInfo(AlarmTimerEntity entity, AlarmTimerRepository repository, DispatcherQueue dispatcherQueue, Action<AlarmTimerRunningInfo> onAlarmTrigger)
    {
        _entity = entity;
        _repository = repository;
        _dispatcherQueue = dispatcherQueue;
        _onAlarmTrigger = onAlarmTrigger;
        _timer = _dispatcherQueue.CreateTimer();        
        _timeOfDay = _entity.TimeOfDay;
        _enabledDayOfWeeks = _entity.EnabledDayOfWeeks;
        _isEnabled = _entity.IsEnabled;
        _title = _entity.Title;
        _snooze = _entity.Snooze;
        _soundSourceType = _entity.SoundSourceType;
        _soundContent = _entity.SoundContent;

        ResetTimer();
    }


    private readonly AlarmTimerRepository _repository;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action<AlarmTimerRunningInfo> _onAlarmTrigger;
    private readonly DispatcherQueueTimer _timer;

    internal AlarmTimerEntity _entity;

    [ObservableProperty]
    private TimeOnly _timeOfDay;

    partial void OnTimeOfDayChanged(TimeOnly value)
    {
        _entity.TimeOfDay = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
        ResetTimer();
    }

    [ObservableProperty]
    private DayOfWeek[] _enabledDayOfWeeks;

    partial void OnEnabledDayOfWeeksChanged(DayOfWeek[] value)
    {
        _entity.EnabledDayOfWeeks = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
        ResetTimer();
    }

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        _entity.IsEnabled = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
        ResetTimer();
    }

    [ObservableProperty]
    private string _title;

    partial void OnTitleChanged(string value)
    {
        _entity.Title = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
    }

    [ObservableProperty]
    private TimeSpan? _snooze;

    partial void OnSnoozeChanged(TimeSpan? value)
    {
        _entity.Snooze = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
    }

    [ObservableProperty]
    private SoundSourceType _soundSourceType;

    partial void OnSoundSourceTypeChanged(SoundSourceType value)
    {
        _entity.SoundSourceType = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
    }

    [ObservableProperty]
    private string _soundContent;

    partial void OnSoundContentChanged(string value)
    {
        _entity.SoundContent = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
    }

    protected override void OnDeferUpdate()
    {
        Save();
        ResetTimer();
    }

    private void Save()
    {
        _repository.UpdateItem(_entity);
    }
    
    private void ResetTimer()
    {
        StopTimer();

        if (IsEnabled)
        {
            CulcTargetTime();

            _timer.Interval = TargetTime - DateTime.Now;
            _timer.IsRepeating = false;
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
    }

    [ObservableProperty]
    private DateTime _targetTime;

    [ObservableProperty]
    private bool _isAlarmChecked;

    public void AlarmChecked(TimeSpan? snoozeTime = null)
    {
        if (EnabledDayOfWeeks.Any() is false)
        {
            IsEnabled = false;
        }

        if (TimeSpan.Zero < snoozeTime)
        {
            _timer.Interval = snoozeTime.Value;
            _timer.Tick += OnSnoozeTick;
            _timer.IsRepeating = true;
            _timer.Start();
        }
        else
        {
            ResetTimer();
        }
    }    

    private void CulcTargetTime()
    {        
        TargetTime = TimeHelpers.CulcNextTime(DateTime.Now, TimeOfDay.ToTimeSpan(), EnabledDayOfWeeks);
    }

    private void StopTimer()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _timer.Tick -= OnSnoozeTick;
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        _onAlarmTrigger(this);
      
        CulcTargetTime();
    }

    private void OnSnoozeTick(DispatcherQueueTimer sender, object args)
    {
        _onAlarmTrigger(this);
    }
}
