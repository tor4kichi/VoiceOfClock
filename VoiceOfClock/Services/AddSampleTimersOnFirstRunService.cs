using CommunityToolkit.WinUI.Helpers;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Services;

public class AddSampleTimersOnFirstRunService : IApplicationLifeCycleAware
{
    private readonly AlarmTimerLifetimeManager _alarmTimerLifetimeManager;
    private readonly OneShotTimerLifetimeManager _oneShotTimerLifetimeManager;
    private readonly PeriodicTimerLifetimeManager _periodicTimerLifetimeManager;

    public AddSampleTimersOnFirstRunService(
        AlarmTimerLifetimeManager alarmTimerLifetimeManager
        , OneShotTimerLifetimeManager oneShotTimerLifetimeManager
        , PeriodicTimerLifetimeManager periodicTimerLifetimeManager)
    {
        _alarmTimerLifetimeManager = alarmTimerLifetimeManager;
        _oneShotTimerLifetimeManager = oneShotTimerLifetimeManager;
        _periodicTimerLifetimeManager = periodicTimerLifetimeManager;
    }
    void IApplicationLifeCycleAware.Initialize()
    {
        if (SystemInformation.Instance.IsFirstRun)
        {
            _alarmTimerLifetimeManager.CreateAlarmTimer("AlarmTimer_TemporaryTitle".Translate(1), TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), Enum.GetValues<DayOfWeek>(), null, SoundSourceType.System, WindowsNotificationSoundType.Reminder.ToString(), isEnabled: false);
            _periodicTimerLifetimeManager.CreatePeriodicTimer("PeriodicTimer_TemporaryTitle".Translate(1), TimeSpan.FromHours(9), TimeSpan.FromHours(10), TimeSpan.FromMinutes(5), Enum.GetValues<DayOfWeek>(), isEnabled: false);
            _oneShotTimerLifetimeManager.CreateTimer("OneShotTimer_TemporaryTitle".Translate(1), TimeSpan.FromMinutes(3), SoundSourceType.System, WindowsNotificationSoundType.Reminder.ToString());
        }
    }

    void IApplicationLifeCycleAware.Resuming()
    {
        
    }

    void IApplicationLifeCycleAware.Suspending()
    {
        
    }
}
