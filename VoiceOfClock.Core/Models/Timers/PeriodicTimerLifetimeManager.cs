using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;

namespace VoiceOfClock.Core.Models.Timers;

public sealed class PeriodicTimerLifetimeManager 
    : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
{
    private readonly IMessenger _messenger;
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly ITimeTriggerService _timeTriggerService;
    private readonly PeriodicTimerRepository _periodicTimerRepository;

    private const string TimeTriggerGroupId = nameof(PeriodicTimerLifetimeManager);

    public PeriodicTimerLifetimeManager(
        IMessenger messenger,
        ISoundContentPlayerService soundContentPlayerService,
        ITimeTriggerService timeTriggerService,
        PeriodicTimerRepository periodicTimerRepository
        )
    {
        _messenger = messenger;
        _soundContentPlayerService = soundContentPlayerService;
        _timeTriggerService = timeTriggerService;
        _periodicTimerRepository = periodicTimerRepository;

        _timeTriggerService.TimeTriggered += OnTimeTriggered;
    }

    private void OnTimeTriggered(object? sender, TimeTriggeredEventArgs e)
    {
        if (e.GroupId != TimeTriggerGroupId) { return; }
        if (Guid.TryParse(e.Id, out Guid timerId) is false) { return; }

        var timer = timerId == InstantPeriodicTimerId ? InstantPeriodicTimer : _periodicTimerRepository.FindById(timerId);        
        Guard.IsNotNull(timer);
        if (TimerIsInsidePeriod(timer) is false)
        {
            _ = SendCurrentTimeVoiceAsync(e.TriggerTime);
            Debug.WriteLine($"ピリオドダイマー： {timer.Title} が完了");

            // 繰り返しが無い場合は無効に切り替える
            if (timer.EnabledDayOfWeeks.Any() is false)
            {
                timer.IsEnabled = false;
                _periodicTimerRepository.UpdateItem(timer);
                _messenger.Send(new PeriodicTimerUpdatedMessage(timer));
            }
        }
        else
        {
            // 次に通知すべき時間を割り出す
            _ = SendCurrentTimeVoiceAsync(e.TriggerTime);
            Debug.WriteLine($"ピリオドダイマー： {timer.Title} の再生を開始");

            _timeTriggerService.SetTimeTrigger(e.Id.ToString(), e.TriggerTime + timer.IntervalTime, TimeTriggerGroupId);
            _messenger.Send(new PeriodicTimerProgressPeriodMessage(timer));
        }
    }

    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);

        

        _timeTriggerService.SetTimeTriggerGroup(TimeTriggerGroupId, GetTimers().Select(x => (x.Id.ToString(), GetNextTime(x))));
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

    private Task SendCurrentTimeVoiceAsync(DateTime time)
    {
        return _soundContentPlayerService.PlayTimeOfDayAsync(time);
    }

    private IEnumerable<PeriodicTimerEntity> GetInsideTimers()
    {
        DateTime now = DateTime.Now;
        return _periodicTimerRepository.ReadAllItems().Where(x => TimerIsInsidePeriod(x, now));
    }

    public IEnumerable<PeriodicTimerEntity> GetTimers()
    {
        return _periodicTimerRepository.ReadAllItems();
    }

    public int GetTimerCount()
    {
        return _periodicTimerRepository.Count();
    }

    private IEnumerable<PeriodicTimerEntity> GetInsideEnablingTimeTimers()
    {
        if (InstantPeriodicTimer.IsEnabled)
        {
            yield return InstantPeriodicTimer;
        }

        foreach (var timer in GetInsideTimers().Where(x => x.IsEnabled))
        {
            yield return timer;
        }
    }        

    public PeriodicTimerEntity CreatePeriodicTimer(string title, TimeSpan startTime, TimeSpan endTime, TimeSpan intervalTime, DayOfWeek[] enabledDayOfWeeks, bool isEnabled = true)
    {
        var entity = _periodicTimerRepository.CreateItem(new PeriodicTimerEntity 
        {
            Title = title,
            EndTime = endTime,
            StartTime = startTime,
            IntervalTime = intervalTime,
            IsEnabled = isEnabled,
            EnabledDayOfWeeks = enabledDayOfWeeks,
            Order = int.MaxValue,
        });

        // 並び順を確実に指定する
        foreach (var (timer, index) in _periodicTimerRepository.ReadAllItems().OrderBy(x => x.Order).Select((x, i) => (x, i)))
        {
            timer.Order = index;
            _periodicTimerRepository.UpdateItem(timer);
        }

        if (entity.IsEnabled)
        {
            _timeTriggerService.SetTimeTrigger(entity.Id.ToString(), GetNextTime(entity), TimeTriggerGroupId);
        }

        return entity;
    }
   
    public bool DeletePeriodicTimer(PeriodicTimerEntity entity)
    {
        if (IsInstantPeriodicTimer(entity)) { return false; }

        var deleted1 = _periodicTimerRepository.DeleteItem(entity.Id);
        _timeTriggerService.DeleteTimeTrigger(entity.Id.ToString(), TimeTriggerGroupId);
        return deleted1;
    }

    public void UpdatePeriodicTimer(PeriodicTimerEntity entity)
    {
        if (IsInstantPeriodicTimer(entity)) { return; }

        _periodicTimerRepository.UpdateItem(entity);
        _timeTriggerService.SetTimeTrigger(entity.Id.ToString(), GetNextTime(entity), TimeTriggerGroupId);
    }



    private static readonly Guid InstantPeriodicTimerId = Guid.Parse("DB31A3F3-AEE1-4C66-8C6D-B0ECAE756524");
    public PeriodicTimerEntity InstantPeriodicTimer { get; } = new PeriodicTimerEntity()
    {
        Id = InstantPeriodicTimerId,
        IsEnabled = false,
        SoundSourceType = SoundSourceType.DateTimeToSpeech,
        SoundContent = string.Empty,
        StartTime = TimeSpan.Zero,
        EndTime = TimeSpan.FromDays(1) - TimeSpan.FromSeconds(1),
    };

    public bool IsInstantPeriodicTimer(PeriodicTimerEntity entity) => entity.Id == InstantPeriodicTimerId;

    public async ValueTask<bool> GetNowInstantPeriodicTimerEnabled()
    {
        return await _timeTriggerService.GetTimeTrigger(InstantPeriodicTimerId.ToString()) != null;
    }

    public void StartInstantPeriodicTimer(TimeSpan intervalTime)
    {
        var now = DateTime.Now;
        InstantPeriodicTimer.IntervalTime = intervalTime;
        InstantPeriodicTimer.StartTime = now.TimeOfDay;
        InstantPeriodicTimer.EndTime = (now - TimeSpan.FromSeconds(1)).TimeOfDay;
        TimeSpan timeOfDay = now.TimeOfDay;        
        _timeTriggerService.SetTimeTrigger(
            InstantPeriodicTimerId.ToString(),
            now.Date + timeOfDay.TrimMilliSeconds(), 
            TimeTriggerGroupId
            );
        _messenger.Send(new PeriodicTimerUpdatedMessage(InstantPeriodicTimer));
    }

    public void StopInstantPeriodicTimer()
    {
        _timeTriggerService.DeleteTimeTrigger(InstantPeriodicTimerId.ToString(), TimeTriggerGroupId);
    }




    public static DateTime GetNextTime(PeriodicTimerEntity entity)
    {
        return GetNextTime(entity, DateTime.Now);
    }

    public static DateTime GetNextTime(PeriodicTimerEntity entity, DateTime targetTime)
    {
        if (TimerIsInsidePeriod(entity, targetTime))
        {
            var (_, _, nextTime) = InsidePeriodCulcNextTime(entity, targetTime);
            return nextTime;
        }
        else
        {
            return OutsideCulcNextTime(entity);
        }
    }

    public static bool TimerIsInsidePeriod(PeriodicTimerEntity entity)
    {
        return TimeHelpers.IsInsideTime(DateTime.Now.TimeOfDay, entity.StartTime, entity.EndTime);
    }

    public static bool TimerIsInsidePeriod(PeriodicTimerEntity entity, DateTime targetTime)
    {
        return TimeHelpers.IsInsideTime(targetTime.TimeOfDay, entity.StartTime, entity.EndTime);
    }


    public static (DateTime startDateTime, TimeSpan elapsedTime, DateTime nextTime) InsidePeriodCulcNextTime(PeriodicTimerEntity entity)
    {
        return InsidePeriodCulcNextTime(entity, DateTime.Now);
    }

    public static (DateTime startDateTime, TimeSpan elapsedTime, DateTime nextTime) InsidePeriodCulcNextTime(PeriodicTimerEntity entity, DateTime targetTime)
    {
        DateTime now = targetTime;
        var startDateTime = now.Date + entity.StartTime;
        if (entity.StartTime > now.TimeOfDay)
        {
            startDateTime -= TimeSpan.FromDays(1);
        }

        var elapsedTime = (now - startDateTime).TrimMilliSeconds();
        int count = (int)Math.Ceiling(elapsedTime / entity.IntervalTime);
        var nextTime = now.Date + entity.StartTime + entity.IntervalTime * count;

        return (startDateTime, elapsedTime, nextTime);
    }

    public static DateTime OutsideCulcNextTime(PeriodicTimerEntity entity, DateTime? targetTime = null)
    {
        return TimeHelpers.CulcNextTime(targetTime ?? DateTime.Now, entity.StartTime, entity.EnabledDayOfWeeks);
    }


}

