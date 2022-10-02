using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using I18NPortable;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Models.Services;

namespace VoiceOfClock.UseCases
{
    public class TimerRunningInfo
    {
        public TimerRunningInfo(PeriodicTimerEntity entity)
        {
            Entity = entity;
            CalcNextTime();
        }

        public PeriodicTimerEntity Entity { get; init; }
        public DateTime NextTime { get; private set; }        
        public bool IsInsidePeriod { get; private set; }

        void CalcNextTime()
        {
            DateTime now = DateTime.Now;
            IsInsidePeriod = TimeHelpers.IsInsideTime(now.TimeOfDay, Entity.StartTime, Entity.EndTime);
            if (IsInsidePeriod)
            {
                TimeSpan elapsedTime = Entity.StartTime - now.TimeOfDay;
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
        IRecipient<PeriodicTimerAdded>

    {
        private readonly IMessenger _messenger;
        private readonly PeriodicTimerRepository _periodicTimerRepository;
        private readonly List<TimerRunningInfo> _periodicTimers;
        private readonly DispatcherQueueTimer _timer;

        public TimerLifetimeManager(IMessenger messenger,
            PeriodicTimerRepository periodicTimerRepository
            )
        {
            _messenger = messenger;
            _periodicTimerRepository = periodicTimerRepository;
            _periodicTimers = _periodicTimerRepository.ReadAllItems().Select(x => new TimerRunningInfo(x)).ToList();
            _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.IsRepeating = true;
            _timer.Tick += _timer_Tick;
            _timer.Start();
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
