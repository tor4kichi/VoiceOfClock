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
        Entity = entity;
        _repository = repository;
        _dispatcherQueue = dispatcherQueue;
        _onAlarmTrigger = onAlarmTrigger;
        _timer = _dispatcherQueue.CreateTimer();        
        _timeOfDay = Entity.TimeOfDay;
        _enabledDayOfWeeks = Entity.EnabledDayOfWeeks;
        _isEnabled = Entity.IsEnabled;
        _title = Entity.Title;
        _snooze = Entity.Snooze;
        _soundSourceType = Entity.SoundSourceType;
        _soundContent = Entity.SoundContent;

        ResetTimer();
    }


    private readonly AlarmTimerRepository _repository;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action<AlarmTimerRunningInfo> _onAlarmTrigger;
    private readonly DispatcherQueueTimer _timer;

    public AlarmTimerEntity Entity { get; }

    [ObservableProperty]
    private TimeOnly _timeOfDay;

    partial void OnTimeOfDayChanged(TimeOnly value)
    {
        Entity.TimeOfDay = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
        ResetTimer();
    }

    [ObservableProperty]
    private DayOfWeek[] _enabledDayOfWeeks;

    partial void OnEnabledDayOfWeeksChanged(DayOfWeek[] value)
    {
        Entity.EnabledDayOfWeeks = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
        ResetTimer();
    }

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        Entity.IsEnabled = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
        ResetTimer();
    }

    [ObservableProperty]
    private string _title;

    partial void OnTitleChanged(string value)
    {
        Entity.Title = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
    }

    [ObservableProperty]
    private TimeSpan? _snooze;

    partial void OnSnoozeChanged(TimeSpan? value)
    {
        Entity.Snooze = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
    }

    [ObservableProperty]
    private SoundSourceType _soundSourceType;

    partial void OnSoundSourceTypeChanged(SoundSourceType value)
    {
        Entity.SoundSourceType = value;
        if (NowDeferUpdateRequested) { return; }

        Save();
    }

    [ObservableProperty]
    private string _soundContent;

    partial void OnSoundContentChanged(string value)
    {
        Entity.SoundContent = value;
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
        _repository.UpdateItem(Entity);
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
