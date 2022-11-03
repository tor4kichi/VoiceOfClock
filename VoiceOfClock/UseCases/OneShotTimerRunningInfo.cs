using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using System;
using VoiceOfClock.Models.Domain;

namespace VoiceOfClock.UseCases;

[ObservableObject]
public sealed partial class OneShotTimerRunningInfo : DeferUpdatable, IDisposable, IRunningTimer
{
    private readonly OneShotTimerEntity _entity;
    private readonly OneShotTimerRepository _repository;
    private readonly OneShotTimerRunningRepository _oneShotTimerRunningRepository;
    private readonly IMessenger _messenger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly OneShotTimerRunningEntity _oneShotTimerRunningEntity;

    private readonly DispatcherQueueTimer _timer;

    public OneShotTimerRunningInfo(OneShotTimerEntity entity, OneShotTimerRepository repository, OneShotTimerRunningRepository oneShotTimerRunningRepository, IMessenger messenger, DispatcherQueue? dispatcherQueue = null)
    {
        _entity = entity;
        _repository = repository;
        _oneShotTimerRunningRepository = oneShotTimerRunningRepository;
        _messenger = messenger;
        _dispatcherQueue = dispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        _timer = _dispatcherQueue.CreateTimer();
        _timer.Tick += OnTimerTick;
        _timer.Interval = TimeSpan.FromSeconds(1d / OneShotTimerConstants.UpdateFPS);

        _oneShotTimerRunningEntity = oneShotTimerRunningRepository.FindById(_entity.Id)
            ?? new OneShotTimerRunningEntity() { Id = _entity.Id, Time = _entity.Time };

        _remainingTime = _oneShotTimerRunningEntity.Time.TrimMilliSeconds();
        _time = _entity.Time;
        _title = _entity.Title;
        _soundSourceType = _entity.SoundType;
        _parameter = _entity.SoundParameter;
    }

    public Guid EntityId => _entity.Id;

    internal bool DeleteEntity()
    {
        _oneShotTimerRunningRepository.DeleteItem(_entity.Id);
        return _repository.DeleteItem(_entity.Id);
    }

    void Save()
    {
        _repository.UpdateItem(_entity);
        _oneShotTimerRunningRepository.UpdateItem(_oneShotTimerRunningEntity);
    }

    protected override void OnDeferUpdate()
    {
        Save();
    }


    bool _isDisposed;
    public void Dispose()
    {
        if (_isDisposed) { return; }
        _isDisposed = true;

        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }


    [ObservableProperty]
    private string _title;

    partial void OnTitleChanged(string value)
    {
        _entity.Title = value;

        if (NowDeferUpdateRequested) { return; }

        _repository.UpdateItem(_entity);
    }

    [ObservableProperty]
    private TimeSpan _time;

    partial void OnTimeChanged(TimeSpan value)
    {
        _entity.Time = value;
        RemainingTime = value;

        if (NowDeferUpdateRequested) { return; }

        _repository.UpdateItem(_entity);
    }

    [ObservableProperty]
    private TimeSpan _remainingTime;

    partial void OnRemainingTimeChanged(TimeSpan value)
    {
        _oneShotTimerRunningEntity.Time = value;

        if (NowDeferUpdateRequested) { return; }
        _oneShotTimerRunningRepository.UpdateItem(_oneShotTimerRunningEntity);
    }


    [ObservableProperty]
    private SoundSourceType _soundSourceType;

    partial void OnSoundSourceTypeChanged(SoundSourceType value)
    {
        _entity.SoundType = value;

        if (NowDeferUpdateRequested) { return; }

        _oneShotTimerRunningRepository.UpdateItem(_oneShotTimerRunningEntity);
    }


    [ObservableProperty]
    private string _parameter;

    partial void OnParameterChanged(string value)
    {
        _entity.SoundParameter = value;

        if (NowDeferUpdateRequested) { return; }
        _oneShotTimerRunningRepository.UpdateItem(_oneShotTimerRunningEntity);
    }


    [ObservableProperty]
    private DateTime _startTime;


    [ObservableProperty]
    private bool _isRunning;

    public void RewindTimer()
    {
        if (IsRunning)
        {
            RemainingTime = Time;
            StartTime = DateTime.Now;
        }
        else
        {
            RemainingTime = Time;
        }
    }

    public event EventHandler<OneShotTimerRunningInfo>? OnTimesUp;

    public void StartTimer()
    {
        if (RemainingTime <= TimeSpan.Zero)
        {
            RemainingTime = Time;
        }

        StartTime = DateTime.Now - (Time - RemainingTime);
        IsRunning = true;

        _timer.Start();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (UpdateRemainingTime() is false)
        {
            sender.Stop();
        }
    }

    public void StopTimer()
    {
        IsRunning = false;
        _timer?.Stop();
    }

    public bool UpdateRemainingTime()
    {
        if (IsRunning)
        {
            var old = RemainingTime;
            var realRemainingTime = Time - (DateTime.Now - _startTime);
            RemainingTime = realRemainingTime;

            if (realRemainingTime <= TimeSpan.Zero)
            {
                IsRunning = false;                
                OnTimesUp?.Invoke(this, this);
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            RemainingTime = Time;
            return false;
        }
    }
}
