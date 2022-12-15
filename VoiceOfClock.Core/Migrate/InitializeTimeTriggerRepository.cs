using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Infrastructure;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.Core.Migrate;

public sealed class InitializeTimeTriggerRepositoryMigrator : IMigrator
{
    private readonly MigrateFlags _flags;
    private readonly ITimeTriggerService _timeTriggerService;
    private readonly AlarmTimerRepository _alarmTimerRepository;
    private readonly PeriodicTimerRepository _periodicTimerRepository;

    public InitializeTimeTriggerRepositoryMigrator(
        ITimeTriggerService timeTriggerService
        , AlarmTimerRepository alarmTimerRepository
        , PeriodicTimerRepository periodicTimerRepository
        )
    {
        _flags = new MigrateFlags();
        _timeTriggerService = timeTriggerService;
        _alarmTimerRepository = alarmTimerRepository;
        _periodicTimerRepository = periodicTimerRepository;
    }

    public Version TargetVersion { get; } = new Version(1, 0, 4, int.MaxValue);
    
    void IMigrator.Migrate()
    {
        DateTime now = DateTime.Now;
        _timeTriggerService.SetTimeTriggerGroup(AlarmTimerLifetimeManager.TimeTriggerGroupId,
           _alarmTimerRepository.ReadAllItems()
           .Where(x => x.IsEnabled)
           .Select(x => (x.Id, TimeHelpers.CulcNextTime(now, x.TimeOfDay.ToTimeSpan(), x.EnabledDayOfWeeks)))
           );

        _timeTriggerService.SetTimeTriggerGroup(PeriodicTimerLifetimeManager.TimeTriggerGroupId,
            _periodicTimerRepository.ReadAllItems()
            .Where(x => x.IsEnabled)
            .Select(x => (x.Id, PeriodicTimerLifetimeManager.GetNextTime(x)))
            );
    }
}
