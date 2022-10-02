using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Models.Services;

namespace VoiceOfClock.UseCases
{
    public sealed partial class TimerRunningInfo : ObservableObject
    {
        public TimerRunningInfo(PeriodicTimerEntity entity)
        {
            Entity = entity;
            CalcNextTime();
        }

        public PeriodicTimerEntity Entity { get; init; }

        [ObservableProperty]
        private DateTime _nextTime;

        [ObservableProperty]
        private bool _isInsidePeriod;
        
        public void UpdateEntity(PeriodicTimerEntity entity)
        {
            Entity.Title = entity.Title;
            Entity.StartTime = entity.StartTime;
            Entity.EndTime = entity.EndTime;
            Entity.IntervalTime = entity.IntervalTime;
            Entity.IsEnabled = entity.IsEnabled;
            CalcNextTime();
        }

        void CalcNextTime()
        {
            DateTime now = DateTime.Now;
            IsInsidePeriod = TimeHelpers.IsInsideTime(now.TimeOfDay, Entity.StartTime, Entity.EndTime);
            if (IsInsidePeriod)
            {
                TimeSpan elapsedTime = now.TimeOfDay - Entity.StartTime;
                int count = (int)Math.Ceiling(elapsedTime / Entity.IntervalTime);
                NextTime = DateTime.Today + Entity.StartTime + Entity.IntervalTime * count;
            }
            else
            {
                if (Entity.StartTime > now.TimeOfDay)
                {
                    NextTime = DateTime.Today + Entity.StartTime;
                }
                else
                {
                    NextTime = DateTime.Today + Entity.StartTime + TimeSpan.FromDays(1);
                }
            }
        }

        public void IncrementNextTime()
        {
            if (DateTime.Now - NextTime < TimeSpan.FromMinutes(1))
            {

            }

            NextTime += Entity.IntervalTime;
        }
    }

    public sealed class TimerLifetimeManager : IApplicationLifeCycleAware,
        IRecipient<PeriodicTimerUpdated>,
        IRecipient<PeriodicTimerRemoved>,
        IRecipient<PeriodicTimerAdded>,
        IRecipient<RequestRunningPeriodicTimer>

    {
        private readonly IMessenger _messenger;
        private readonly PeriodicTimerRepository _periodicTimerRepository;
        private readonly ObservableCollection<TimerRunningInfo> _periodicTimers;
        private readonly DispatcherQueueTimer _timer;

        IDisposable _runningTimerObserver;

        public TimerLifetimeManager(IMessenger messenger,
            PeriodicTimerRepository periodicTimerRepository
            )
        {
            _messenger = messenger;
            _periodicTimerRepository = periodicTimerRepository;
            _periodicTimers = new ObservableCollection<TimerRunningInfo>(_periodicTimerRepository.ReadAllItems().Select(x => new TimerRunningInfo(x)));
            _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.IsRepeating = true;
            _timer.Tick += _timer_Tick;
            _timer.Start();

            _runningTimerObserver = new[]
            {
                _periodicTimers.ObserveElementProperty(x => x.NextTime).Select(x => x.Instance),
                _periodicTimers.ObserveElementProperty(x => x.IsInsidePeriod).Select(x => x.Instance)
            }
            .Merge()            
            .Subscribe(x => _messenger.Send<RunningPeriodicTimerUpdated>(new RunningPeriodicTimerUpdated(x)));            
        }

        private void _timer_Tick(DispatcherQueueTimer sender, object args)
        {
            if (GetInsideEnablingTimeTimers() is not null and var timers && timers.Any())
            {
                foreach (var timer in timers)
                {
                    // 次に通知すべき時間を割り出す
                    if (timer.NextTime < DateTime.Now + TimeSpan.FromSeconds(1))
                    {
                        var time = timer.NextTime;
                        _ = SendCurrentTimeVoiceAsync(time);
                        timer.IncrementNextTime();

                        Debug.WriteLine($"{timer.Entity.Title} の再生を開始");
                    }
                }
            }

            Debug.WriteLine(DateTime.Now.TimeOfDay);
        }

        async Task SendCurrentTimeVoiceAsync(DateTime time)
        {
            var result = await _messenger.Send(new TimeOfDayPlayVoiceRequest(new(time)));            
        }

        IEnumerable<TimerRunningInfo> GetInsideEnablingTimeTimers()
        {
            TimeSpan timeOfDay = DateTime.Now.TimeOfDay;
            return _periodicTimers.Where(x => x.Entity.IsEnabled && TimeHelpers.IsInsideTime(timeOfDay, x.Entity.StartTime, x.Entity.EndTime));
        }        

        void IApplicationLifeCycleAware.Initialize()
        {
            _messenger.RegisterAll(this);
        }


        void IApplicationLifeCycleAware.Resuming()
        {
        }

        void IApplicationLifeCycleAware.Suspending()
        {
        }


        void IRecipient<PeriodicTimerUpdated>.Receive(PeriodicTimerUpdated message)
        {
            var entity = message.Value;
            _periodicTimers.First(x => x.Entity.Id == entity.Id).UpdateEntity(entity);
        }

        void IRecipient<PeriodicTimerRemoved>.Receive(PeriodicTimerRemoved message)
        {           
            var target = _periodicTimers.First(x => x.Entity.Id == message.Value.Id);
            _periodicTimers.Remove(target);
        }

        void IRecipient<PeriodicTimerAdded>.Receive(PeriodicTimerAdded message)
        {
            _periodicTimers.Add(new TimerRunningInfo(message.Value));
        }

        void IRecipient<RequestRunningPeriodicTimer>.Receive(RequestRunningPeriodicTimer message)
        {
            message.Reply(_periodicTimers.First(x => x.Entity.Id == message.TimerId));
        }
    }

    public sealed class RunningPeriodicTimerUpdated : ValueChangedMessage<TimerRunningInfo>
    {
        public RunningPeriodicTimerUpdated(TimerRunningInfo value) : base(value)
        {
        }
    }

    public sealed class RequestRunningPeriodicTimer : RequestMessage<TimerRunningInfo>
    {
        public RequestRunningPeriodicTimer(Guid timerId)
        {
            TimerId = timerId;
        }

        public Guid TimerId { get; }
    }


    public sealed class PeriodicTimerUpdated : ValueChangedMessage<PeriodicTimerEntity>
    {
        public PeriodicTimerUpdated(PeriodicTimerEntity value) : base(value)
        {
        }
    }

    public sealed class PeriodicTimerRemoved : ValueChangedMessage<PeriodicTimerEntity>
    {
        public PeriodicTimerRemoved(PeriodicTimerEntity value) : base(value)
        {
        }
    }

    public sealed class PeriodicTimerAdded : ValueChangedMessage<PeriodicTimerEntity>
    {
        public PeriodicTimerAdded(PeriodicTimerEntity value) : base(value)
        {
        }
    }
}
