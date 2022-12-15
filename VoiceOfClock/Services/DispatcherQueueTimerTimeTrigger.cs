using Microsoft.UI.Dispatching;
using System;
using System.Diagnostics;
using VoiceOfClock.Core.Contracts.Services;

namespace VoiceOfClock.Services;

public sealed class DispatcherQueueTimerTimeTrigger : ISingleTimeTrigger
{
    private readonly DispatcherQueueTimer _dispatcherQueueTimer;

    public DispatcherQueueTimerTimeTrigger()        
    {
        _dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _dispatcherQueueTimer.IsRepeating = false;
        _dispatcherQueueTimer.Tick += (s, e) =>
        {
            OnTimeArrived(_lastTriggerTime!.Value, _lastArgument!.Value);
        };
    }
    
    public event EventHandler<ISingleTimeTrigger.TimeTriggerRecievedEventArgs>? TimeArrived;

    DateTime? _lastTriggerTime;
    Guid? _lastArgument;
    void ISingleTimeTriggerBase<Guid>.SetTimeTrigger(DateTime triggerTime, Guid argument)
    {
        _dispatcherQueueTimer.Stop();

        DateTime now = DateTime.Now;
        if (triggerTime < now)
        {
            Debug.WriteLine($"[DispatcherQueueTimerTimeTrigger] SetTimeTrigger already in Trigger time : {triggerTime}, {argument}");
            _lastTriggerTime = null;
            _lastArgument = null;
            OnTimeArrived(triggerTime, argument);
            return;
        }
        else
        {
            Debug.WriteLine($"[DispatcherQueueTimerTimeTrigger] SetTimeTrigger set next trigger : {triggerTime}, {argument}");
            _lastTriggerTime = triggerTime;
            _lastArgument = argument;
            _dispatcherQueueTimer.Interval = triggerTime - now;
            _dispatcherQueueTimer.Start();
        }
    }

    private void OnTimeArrived(DateTime dateTime, Guid argument)
    {
        Debug.WriteLine($"[DispatcherQueueTimerTimeTrigger] OnTimeArrived: {dateTime}, {argument}");
        TimeArrived?.Invoke(this, new(dateTime, argument));
    }

    void ISingleTimeTriggerBase<Guid>.Clear()
    {
        Debug.WriteLine($"[DispatcherQueueTimerTimeTrigger] Clear: ");
        _dispatcherQueueTimer.Stop();
        _lastTriggerTime = null;
        _lastArgument = null;
    }
}
