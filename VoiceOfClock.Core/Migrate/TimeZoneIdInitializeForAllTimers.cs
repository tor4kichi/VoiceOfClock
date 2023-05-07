using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Infrastructure;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Core.Migrate;
public sealed class TimeZoneIdInitializeForAllTimers : IMigrator
{
    private readonly AlarmTimerRepository _alarmTimerRepository;
    private readonly OneShotTimerRepository _oneShotTimerRepository;
    private readonly PeriodicTimerRepository _periodicTimerRepository;

    public TimeZoneIdInitializeForAllTimers(
        AlarmTimerRepository alarmTimerRepository,
        OneShotTimerRepository oneShotTimerRepository,
        PeriodicTimerRepository periodicTimerRepository
        )
    {
        _alarmTimerRepository = alarmTimerRepository;
        _oneShotTimerRepository = oneShotTimerRepository;
        _periodicTimerRepository = periodicTimerRepository;
    }

    public Version TargetVersion { get; } = new Version(1, 1, 4, int.MaxValue);

    public void Migrate()
    {
        string currentTimeZoneId = TimeZoneInfo.Local.Id;
        foreach (var alarm in _alarmTimerRepository.ReadAllItems())
        {
            alarm.TimeZoneId ??= currentTimeZoneId;
            _alarmTimerRepository.UpdateItem(alarm);
        }

        foreach (var timer in _oneShotTimerRepository.ReadAllItems())
        {
            timer.TimeZoneId ??= currentTimeZoneId;
            _oneShotTimerRepository.UpdateItem(timer);
        }

        foreach (var periodicTimer in _periodicTimerRepository.ReadAllItems())
        {
            periodicTimer.TimeZoneId ??= currentTimeZoneId;
            _periodicTimerRepository.UpdateItem(periodicTimer);
        }
    }
}
