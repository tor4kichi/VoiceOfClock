using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using I18NPortable;
using Microsoft.UI.Dispatching;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Models.Services;

namespace VoiceOfClock.UseCases;

public sealed class PeriodicTimerLifetimeManager : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
{
    private readonly IMessenger _messenger;
    private readonly PeriodicTimerRepository _periodicTimerRepository;
    private readonly ObservableCollection<PeriodicTimerRunningInfo> _periodicTimers;
    private readonly DispatcherQueueTimer _timer;

    public ReadOnlyObservableCollection<PeriodicTimerRunningInfo> PeriodicTimers { get; }
    public PeriodicTimerRunningInfo InstantPeriodicTimer { get; }

    public PeriodicTimerLifetimeManager(IMessenger messenger,
        PeriodicTimerRepository periodicTimerRepository
        )
    {
        _messenger = messenger;
        _periodicTimerRepository = periodicTimerRepository;
        _periodicTimers = new ObservableCollection<PeriodicTimerRunningInfo>(_periodicTimerRepository.ReadAllItems().Select(x => new PeriodicTimerRunningInfo(x, _periodicTimerRepository)));
        PeriodicTimers = new(_periodicTimers);
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(0.2);
        _timer.IsRepeating = true;
        _timer.Tick += _timer_Tick;
        _timer.Start();

        InstantPeriodicTimer = new PeriodicTimerRunningInfo(new PeriodicTimerEntity() 
        {
            IsEnabled = false,
            Id = Guid.Empty,
            Title = "InstantPeriodicTimer_Title".Translate(),                
        }, _periodicTimerRepository);
    }


    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);
    }

    void IApplicationLifeCycleAware.Resuming() { }

    void IApplicationLifeCycleAware.Suspending() { }

    void IRecipient<ActiveTimerCollectionRequestMessage>.Receive(ActiveTimerCollectionRequestMessage message)
    {            
        foreach (var timer in GetInsideEnablingTimeTimers())
        {
            message.Reply(timer);
        }
    }


    DateTime _lastTickTime = DateTime.MinValue;
    private void _timer_Tick(DispatcherQueueTimer sender, object args)
    {
        if (GetInsideEnablingTimeTimers() is not null and var timers && timers.Any())
        {
            foreach (var timer in timers)
            {
                // 次に通知すべき時間を割り出す
                if (timer.NextTime < DateTime.Now)
                {
                    if (timer.NextTime.TimeOfDay == timer.StartTime)
                    {
                        _ = SendCurrentTimeVoiceAsync(timer.NextTime);
                        Debug.WriteLine($"ピリオドダイマー： {timer.Title} を開始");
                        
                        timer.IncrementNextTime();
                        //_messenger.Send(new PeriodicTimerUpdated(timer._entity));
                    }
                    else if (timer.NextTime.TimeOfDay == timer.EndTime)
                    {
                        _ = SendCurrentTimeVoiceAsync(timer.NextTime);
                        Debug.WriteLine($"ピリオドダイマー： {timer.Title} が完了");
                        
                        timer.OnEnded();
                        _messenger.Send(new PeriodicTimerUpdated(timer._entity));
                    }
                    else
                    {
                        _ = SendCurrentTimeVoiceAsync(timer.NextTime);
                        Debug.WriteLine($"ピリオドダイマー： {timer.Title} の再生を開始");

                        timer.IncrementNextTime();
                        //_messenger.Send(new PeriodicTimerUpdated(timer._entity));
                    }
                }

                timer.UpdateElapsedTime();
            }
        }

        _lastTickTime = DateTime.Now;
        //Debug.WriteLine(_lastTickTime.TimeOfDay);
    }

    async Task SendCurrentTimeVoiceAsync(DateTime time)
    {
        var result = await _messenger.Send(new TimeOfDayPlayVoiceRequest(time));            
    }

    IEnumerable<PeriodicTimerRunningInfo> GetInsideEnablingTimeTimers()
    {
        if (InstantPeriodicTimer.IsEnabled)
        {
            yield return InstantPeriodicTimer;
        }

        TimeSpan timeOfDay = DateTime.Now.TimeOfDay.TrimMilliSeconds();
        foreach (var timer in _periodicTimers.Where(x => x.IsEnabled && TimeHelpers.IsInsideTime(timeOfDay, x.StartTime, x.EndTime)))
        {
            yield return timer;
        }
    }        

    public PeriodicTimerRunningInfo CreatePeriodicTimer(string title, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime, DayOfWeek[] enabledDayOfWeeks, bool isEnabled = true)
    {
        var entity = _periodicTimerRepository.CreateItem(new PeriodicTimerEntity 
        {
            Title = title,
            EndTime = endTime,
            StartTime = startTime,
            IntervalTime = intervalTime,
            IsEnabled = isEnabled,
            EnabledDayOfWeeks = enabledDayOfWeeks,
        });

        var runningTimerInfo = new PeriodicTimerRunningInfo(entity, _periodicTimerRepository);
        _periodicTimers.Add(runningTimerInfo);        
        return runningTimerInfo;
    }

    public bool DeletePeriodicTimer(PeriodicTimerRunningInfo timer)
    {
        var entity = timer._entity;
        var deleted1 = _periodicTimerRepository.DeleteItem(entity.Id);
        var deleted2 = _periodicTimers.Remove(timer);        
        return deleted2;
    }


    public void StartInstantPeriodicTimer(TimeSpan intervalTime)
    {
        TimeSpan timeOfDay = DateTime.Now.TimeOfDay;
        timeOfDay = new TimeSpan(timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
        using (InstantPeriodicTimer.DeferUpdate())
        {
            InstantPeriodicTimer.IsEnabled = true;
            InstantPeriodicTimer.IntervalTime = intervalTime;
            InstantPeriodicTimer.StartTime = timeOfDay;
            InstantPeriodicTimer.EndTime = timeOfDay - TimeSpan.FromSeconds(1);
        }
    }

    public void StopInstantPeriodicTimer()
    {
        InstantPeriodicTimer.IsEnabled = false;
    }

}

public sealed class PeriodicTimerUpdated : ValueChangedMessage<PeriodicTimerEntity>
{
    public PeriodicTimerUpdated(PeriodicTimerEntity value) : base(value)
    {
    }
}
