using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Infrastructure;
using VoiceOfClock.Core.Migrate;

namespace VoiceOfClock.Core.Services;

public sealed class MigrationService 
{
    private readonly MigrateFlags _flags;
    public MigrationService(MigrateFlags flags)
    {
        _flags = flags;
    }

    public void ClearMigrateStatus()
    {
        foreach (var (_, id) in EnumerateMigrators())
        {
            _flags[id] = null;
        }
    }
    
    IEnumerable<(IMigrator?, string id)> EnumerateMigrators()
    {
        yield return (Ioc.Default.GetService<TimeTriggerRepositoryEntityIdTypeMigrator>(), "TimeTriggerRepositoryEntityIdTypeMigrator");
        yield return (Ioc.Default.GetService<InitializeTimeTriggerRepositoryMigrator>(), "InitializeTimeTriggerRepositoryMigrator");
        yield return (Ioc.Default.GetService<TimeZoneIdInitializeForAllTimers>(), "TimeZoneIdInitializeForAllTimers");        
    }

    public void Migrate(Version currentVersion, Version prevVersion)
    {
        foreach (var (migrator, id) in EnumerateMigrators())
        {
            if (migrator == null) { continue; }
            if (!IsVersionLaunched(migrator.TargetVersion, prevVersion)) { continue; }
            
            if (_flags[id] != null) { continue; }
            {
                try
                {
                    migrator.Migrate();
                    _flags[id] = true;
                }
                catch
                {
                    _flags[id] = false;
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="target">v1.0.4 を判定したい場合は revision に int.MaxValue を入れないと判定されない</param>
    /// <param name="lastVersion"></param>
    /// <returns></returns>
    static bool IsVersionLaunched(Version target, Version? lastVersion)
    {
        return lastVersion != null
            ? lastVersion <= target
            : false
            ;
    }
}
