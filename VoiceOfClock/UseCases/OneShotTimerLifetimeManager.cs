using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;

namespace VoiceOfClock.UseCases;


static class OneShotTimerConstants
{
    public const int UpdateFPS = 6;
}

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

    public OneShotTimerRunningInfo(OneShotTimerEntity entity, OneShotTimerRepository repository, OneShotTimerRunningRepository oneShotTimerRunningRepository, IMessenger messenger,  DispatcherQueue? dispatcherQueue = null)
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



    Action<OneShotTimerRunningInfo>? _onTimesUpAction;
    Action<TimeSpan>? _onRemainingTimeUpdated;

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

    public void StartTimer(Action<TimeSpan> onRemainingTimeUpdated, Action<OneShotTimerRunningInfo> onTimesUp)
    {
        if (RemainingTime <= TimeSpan.Zero)
        {
            RemainingTime = Time;
        }

        StartTime = DateTime.Now - (Time - RemainingTime);
        _onRemainingTimeUpdated = onRemainingTimeUpdated;
        _onRemainingTimeUpdated?.Invoke(RemainingTime);
        _onTimesUpAction = onTimesUp;
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
        _onTimesUpAction = null;
        _timer?.Stop();
    }

    public bool UpdateRemainingTime()
    {
        if (IsRunning)
        {
            var old = RemainingTime;
            var realRemainingTime = Time - (DateTime.Now - _startTime);
            RemainingTime = realRemainingTime;
            if (old != RemainingTime)
            {
                _onRemainingTimeUpdated?.Invoke(RemainingTime);
            }

            if (realRemainingTime <= TimeSpan.Zero)
            {
                IsRunning = false;
                _onTimesUpAction?.Invoke(this);
                if (_entity.SoundType == SoundSourceType.System)
                {
                    _messenger.Send(new PlaySystemSoundRequest(Enum.Parse<WindowsNotificationSoundType>( _entity.SoundParameter)));
                }
                else if (_entity.SoundType == SoundSourceType.Tts)
                {
                    _messenger.Send(new TextPlayVoiceRequest(_entity.SoundParameter));
                }
                else if (_entity.SoundType == SoundSourceType.TtsWithSSML)
                {
                    _messenger.Send(new SsmlPlayVoiceRequest(_entity.SoundParameter));
                }

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

public sealed class OneShotTimerLifetimeManager : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
{
    private readonly IMessenger _messenger;
    private readonly OneShotTimerRepository _oneShotTimerRepository;
    private readonly OneShotTimerRunningRepository _oneShotTimerRunningRepository;

    public OneShotTimerLifetimeManager(
        IMessenger messenger,
        OneShotTimerRepository oneShotTimerRepository,
        OneShotTimerRunningRepository oneShotTimerRunningRepository
        )
    {
        _messenger = messenger;
        _oneShotTimerRepository = oneShotTimerRepository;
        _oneShotTimerRunningRepository = oneShotTimerRunningRepository;

        _timers = new ObservableCollection<OneShotTimerRunningInfo>();
        Timers = new ReadOnlyObservableCollection<OneShotTimerRunningInfo>(_timers);
    }

    public ReadOnlyObservableCollection<OneShotTimerRunningInfo> Timers { get; }
    public ObservableCollection<OneShotTimerRunningInfo> _timers;

    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);

        var timers = _oneShotTimerRepository.ReadAllItems();
        foreach (var timer in timers)
        {
            _timers.Add(new OneShotTimerRunningInfo(timer, _oneShotTimerRepository, _oneShotTimerRunningRepository, _messenger));
        }
    }

    void IApplicationLifeCycleAware.Resuming()
    {
        
    }

    void IApplicationLifeCycleAware.Suspending()
    {
        
    }

    void IRecipient<ActiveTimerCollectionRequestMessage>.Receive(ActiveTimerCollectionRequestMessage message)
    {
        foreach (var timer in _timers)
        {
            if (timer.IsRunning)
            {
                message.Reply(timer);
            }
        }
    }

    public OneShotTimerRunningInfo CreateTimer(string title, TimeSpan time)
    {
        var entity = _oneShotTimerRepository.CreateItem(new OneShotTimerEntity() { Title = title, Time = time });
        var runningInfo = new OneShotTimerRunningInfo(entity, _oneShotTimerRepository, _oneShotTimerRunningRepository, _messenger);
        _timers.Add(runningInfo);
        return runningInfo;
    }


    public void DeleteTimer(OneShotTimerRunningInfo info)
    {
        _oneShotTimerRepository.DeleteItem(info.EntityId);
        _oneShotTimerRunningRepository.DeleteItem(info.EntityId);

        foreach (var remItem in _timers.Where(x => x.EntityId == info.EntityId).ToArray())
        {
            _timers.Remove(remItem);
        }
    }

}
