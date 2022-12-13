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
            OnTimeArrived(_lastTriggerTime!.Value, _lastArgument!);
        };
    }

    DateTime? _lastTriggerTime;
    string? _lastArgument;
    public void SetTimeTrigger(DateTime triggerTime, string argument)
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

    private void OnTimeArrived(DateTime dateTime, string argument)
    {
        Debug.WriteLine($"[DispatcherQueueTimerTimeTrigger] OnTimeArrived: {dateTime}, {argument}");
        TimeArrived?.Invoke(this, new(dateTime, argument));
    }

    public void Clear()
    {
        Debug.WriteLine($"[DispatcherQueueTimerTimeTrigger] Clear: ");
        _dispatcherQueueTimer.Stop();
        _lastTriggerTime = null;
        _lastArgument = null;
    }

    public event EventHandler<TimeTriggerRecievedEventArgs>? TimeArrived;
}
