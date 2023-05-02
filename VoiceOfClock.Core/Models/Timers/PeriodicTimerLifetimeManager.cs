using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Services;
using static VoiceOfClock.Core.Contracts.Services.ITimeTriggerServiceBase<System.Guid>;

namespace VoiceOfClock.Core.Models.Timers;

public sealed class PeriodicTimerLifetimeManager 
    : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
{
    private readonly IMessenger _messenger;
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly ITimeTriggerService _timeTriggerService;
    private readonly PeriodicTimerRepository _periodicTimerRepository;

    public const string TimeTriggerGroupId = "Periodic";

    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _timerAudioCancelMap = new ();

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

        var timer = e.Id == InstantPeriodicTimerId ? InstantPeriodicTimer : _periodicTimerRepository.FindById(e.Id);        
        Guard.IsNotNull(timer);

        CancelTimerPlayingVoice(timer, NotifyAudioEndedReason.CancelledFromNextNotify);
        if (DateTime.Now - e.TriggerTime < TimeSpan.FromSeconds(3))
        {            
            _ = SendCurrentTimeVoiceAsync(timer, e.TriggerTime);
            Debug.WriteLine($"ピリオドダイマー： {timer.Title} の再生を開始");
        }

        if (TimerIsInsidePeriod(timer) is false)
        {
            // 繰り返しが無い場合は無効に切り替える
            if (timer.EnabledDayOfWeeks.Any() is false)
            {
                timer.IsEnabled = false;
                _periodicTimerRepository.UpdateItem(timer);
            }

            _messenger.Send(new PeriodicTimerUpdatedMessage(timer, e.TriggerTime));
        }
        else
        {
            // 次に通知すべき時間を割り出す
            _timeTriggerService.SetTimeTrigger(e.Id, e.TriggerTime + timer.IntervalTime, TimeTriggerGroupId);
            _messenger.Send(new PeriodicTimerProgressPeriodMessage(timer, e.TriggerTime));
        }
    }


    public void StopNotifyAudio(PeriodicTimerEntity entity)
    {
        CancelTimerPlayingVoice(entity, NotifyAudioEndedReason.CancelledByUser);
    }

    private void CancelTimerPlayingVoice(PeriodicTimerEntity entity, NotifyAudioEndedReason endedReason)
    {
        if (_timerAudioCancelMap.Remove(entity.Id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();

            _messenger.Send(new NotifyAudioEndedMessage(entity, endedReason));
        }
    }

    private async Task SendCurrentTimeVoiceAsync(PeriodicTimerEntity entity, DateTime time)
    {
        var cts = new CancellationTokenSource();
        _timerAudioCancelMap.TryAdd(entity.Id, cts);
        try
        {
            _messenger.Send(new NotifyAudioStartingMessage(entity));
            await _soundContentPlayerService.PlayTimeOfDayAsync(time, cancellationToken: cts.Token);
            CancelTimerPlayingVoice(entity, NotifyAudioEndedReason.Completed);
        }
        catch (OperationCanceledException)
        {
            // do nothing.
        }
        catch
        {
            CancelTimerPlayingVoice(entity, NotifyAudioEndedReason.Unknown);
            throw;
        }
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
            _timeTriggerService.SetTimeTrigger(entity.Id, GetNextTime(entity), TimeTriggerGroupId);
        }

        return entity;
    }
   
    public bool DeletePeriodicTimer(PeriodicTimerEntity entity)
    {
        if (IsInstantPeriodicTimer(entity)) { return false; }

        var deleted1 = _periodicTimerRepository.DeleteItem(entity.Id);
        _timeTriggerService.DeleteTimeTrigger(entity.Id, TimeTriggerGroupId);
        return deleted1;
    }

    public void UpdatePeriodicTimer(PeriodicTimerEntity entity)
    {
        if (IsInstantPeriodicTimer(entity)) { return; }

        _periodicTimerRepository.UpdateItem(entity);
        if (entity.IsEnabled)
        {
            _timeTriggerService.SetTimeTrigger(entity.Id, GetNextTime(entity), TimeTriggerGroupId);
        }
        else
        {
            _timeTriggerService.DeleteTimeTrigger(entity.Id, TimeTriggerGroupId);
        }
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

    public bool GetNowInstantPeriodicTimerEnabled()
    {
        return _timeTriggerService.GetTimeTrigger(InstantPeriodicTimerId) != null;
    }

    public void StartInstantPeriodicTimer(TimeSpan intervalTime)
    {
        var now = DateTime.Now;
        DateTime targetTime = now.Date + now.TimeOfDay.TrimMilliSeconds();
        InstantPeriodicTimer.IsEnabled = true;
        InstantPeriodicTimer.IntervalTime = intervalTime;
        InstantPeriodicTimer.StartTime = targetTime.TimeOfDay;
        InstantPeriodicTimer.EndTime = (targetTime - TimeSpan.FromSeconds(1)).TimeOfDay;             
        _timeTriggerService.SetTimeTrigger(
            InstantPeriodicTimerId,
            targetTime,
            TimeTriggerGroupId
            );
        _messenger.Send(new PeriodicTimerUpdatedMessage(InstantPeriodicTimer, targetTime));
    }

    public void StopInstantPeriodicTimer()
    {
        InstantPeriodicTimer.IsEnabled = false;
        _timeTriggerService.DeleteTimeTrigger(InstantPeriodicTimerId, TimeTriggerGroupId);
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
        if (entity.StartTime == targetTime.TimeOfDay)
        {
            return (targetTime, TimeSpan.Zero, targetTime + entity.IntervalTime);
        }

        var startDateTime = targetTime.Date + entity.StartTime;        
        if (entity.StartTime > targetTime.TimeOfDay)
        {
            startDateTime -= TimeSpan.FromDays(1);
        }

        var elapsedTime = (targetTime - startDateTime).TrimMilliSeconds();
        int count = (int)Math.Ceiling(elapsedTime / entity.IntervalTime);
        var nextTime = targetTime.Date + entity.StartTime + entity.IntervalTime * count;

        return (startDateTime, elapsedTime, nextTime);
    }

    public static DateTime OutsideCulcNextTime(PeriodicTimerEntity entity, DateTime? targetTime = null)
    {
        return TimeHelpers.CulcNextTime(targetTime ?? DateTime.Now, entity.StartTime, entity.EnabledDayOfWeeks);
    }


}

