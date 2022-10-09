using CommunityToolkit.Diagnostics;
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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Models.Services;

namespace VoiceOfClock.UseCases
{
    public abstract class DeferUpdatable
    {
        public IDisposable DeferUpdate()
        {
            Guard.IsFalse(NowDeferUpdateRequested, nameof(NowDeferUpdateRequested));

            NowDeferUpdateRequested = true;
            return Disposable.Create(OnDeferUpdate_Internal);
        }

        bool _nowDeferUpdateRequested;
        protected bool NowDeferUpdateRequested
        {
            get => _nowDeferUpdateRequested;
            private set => _nowDeferUpdateRequested = value;
        }

        private void OnDeferUpdate_Internal()
        {
            NowDeferUpdateRequested = false;
            OnDeferUpdate();
        }
        protected abstract void OnDeferUpdate();
    }

    [ObservableObject]
    public sealed partial class PeriodicTimerRunningInfo : DeferUpdatable
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

        public void UpdateEntity(PeriodicTimerEntity entity)
        {
            _entity.Title = entity.Title;
            _entity.StartTime = entity.StartTime;
            _entity.EndTime = entity.EndTime;
            _entity.IntervalTime = entity.IntervalTime;
            _entity.IsEnabled = entity.IsEnabled;
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
                if (_entity.StartTime > now.TimeOfDay)
                {
                    NextTime = DateTime.Today + _entity.StartTime;
                }
                else
                {
                    NextTime = DateTime.Today + _entity.StartTime + TimeSpan.FromDays(1);
                }
            }
        }

        public void IncrementNextTime()
        {
            NextTime += _entity.IntervalTime;
        }

        public void UpdateElapsedTime()
        {
            if (TimeHelpers.IsInsideTime(DateTime.Now.TimeOfDay, _entity.StartTime, _entity.EndTime))
            {                
                ElapsedTime = (DateTime.Now - StartDateTime).TrimMilliSeconds();
            }
            else
            {
                ElapsedTime = TimeSpan.Zero;
            }
        }
    }

    public sealed class TimerLifetimeManager : IApplicationLifeCycleAware
    {
        private readonly IMessenger _messenger;
        private readonly PeriodicTimerRepository _periodicTimerRepository;
        private readonly ObservableCollection<PeriodicTimerRunningInfo> _periodicTimers;
        private readonly DispatcherQueueTimer _timer;

        public ReadOnlyObservableCollection<PeriodicTimerRunningInfo> PeriodicTimers { get; }
        public PeriodicTimerRunningInfo InstantPeriodicTimer { get; }

        public TimerLifetimeManager(IMessenger messenger,
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
                        var time = timer.NextTime;
                        _ = SendCurrentTimeVoiceAsync(time);
                        timer.IncrementNextTime();

                        Debug.WriteLine($"{timer.Title} の再生を開始");
                        _messenger.Send(new PeriodicTimerUpdated(timer._entity));
                    }

                    timer.UpdateElapsedTime();
                }
            }

            _lastTickTime = DateTime.Now;
            //Debug.WriteLine(_lastTickTime.TimeOfDay);
        }

        async Task SendCurrentTimeVoiceAsync(DateTime time)
        {
            var result = await _messenger.Send(new TimeOfDayPlayVoiceRequest(new(time)));            
        }

        IEnumerable<PeriodicTimerRunningInfo> GetInsideEnablingTimeTimers()
        {
            if (InstantPeriodicTimer.IsEnabled)
            {
                yield return InstantPeriodicTimer;
            }

            TimeSpan timeOfDay = DateTime.Now.TimeOfDay;
            foreach (var timer in _periodicTimers.Where(x => x.IsEnabled && TimeHelpers.IsInsideTime(timeOfDay, x.StartTime, x.EndTime)))
            {
                yield return timer;
            }
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

        public PeriodicTimerRunningInfo CreatePeriodicTimer(string title, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime, bool isEnabled = true)
        {
            var entity = _periodicTimerRepository.CreateItem(new PeriodicTimerEntity 
            {
                Title = title,
                EndTime = endTime,
                StartTime = startTime,
                IntervalTime = intervalTime,
                IsEnabled = isEnabled,
            });

            var runningTimerInfo = new PeriodicTimerRunningInfo(entity, _periodicTimerRepository);
            _periodicTimers.Add(runningTimerInfo);
            _messenger.Send(new PeriodicTimerAdded(entity));
            return runningTimerInfo;
        }

        public bool DeletePeriodicTimer(PeriodicTimerRunningInfo timer)
        {
            var entity = timer._entity;
            var deleted1 = _periodicTimerRepository.DeleteItem(entity.Id);
            var deleted2 = _periodicTimers.Remove(timer);
            _messenger.Send(new PeriodicTimerRemoved(entity));
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

    public sealed class RequestRunningPeriodicTimer : RequestMessage<PeriodicTimerRunningInfo>
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
