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
        _dispatcherQueueTimer.IsRepeating = true;
        _dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1);
        _dispatcherQueueTimer.Tick += (s, e) =>
        {
            if (_triggerTime.HasValue is false) 
            {
                _dispatcherQueueTimer.Stop();                
            }            
            else if (_triggerTime.Value < DateTime.Now)
            {
                _dispatcherQueueTimer.Stop();
                var triggerTime = _triggerTime.Value;
                var args = _argument!.Value;
                _triggerTime = null;
                _argument = null;
                OnTimeArrived(triggerTime, args);
            }            
        };
    }
    
    public event EventHandler<ISingleTimeTrigger.TimeTriggerRecievedEventArgs>? TimeArrived;

    DateTime? _triggerTime;
    Guid? _argument;
    void ISingleTimeTriggerBase<Guid>.SetTimeTrigger(DateTime triggerTime, Guid argument)
    {
        _dispatcherQueueTimer.Stop();

        DateTime now = DateTime.Now;
        if (triggerTime < now)
        {
            Debug.WriteLine($"[DispatcherQueueTimerTimeTrigger] SetTimeTrigger already in Trigger time : {triggerTime}, {argument}");
            _triggerTime = null;
            _argument = null;
            OnTimeArrived(triggerTime, argument);
            return;
        }
        else
        {
            Debug.WriteLine($"[DispatcherQueueTimerTimeTrigger] SetTimeTrigger set next trigger : {triggerTime}, {argument}");
            _triggerTime = triggerTime;
            _argument = argument;            
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
        _triggerTime = null;
        _argument = null;
    }
}
