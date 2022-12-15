using LiteDB;
using VoiceOfClock.Core.Infrastructure;
using VoiceOfClock.Core.Services;

namespace VoiceOfClock.Core.Migrate;

/// <summary>
/// Migration since from v1.0.4. <br />
/// TimeTriggerEntity.Id change type from 'string' to 'Guid'.
/// </summary>
public sealed class TimeTriggerRepositoryEntityIdTypeMigrator : IMigrator
{
    private readonly ILiteDatabase _database;

    public TimeTriggerRepositoryEntityIdTypeMigrator(ILiteDatabase database)
    {
        _database = database;
    }

    public Version TargetVersion { get; } = new Version(1, 0, 4, int.MaxValue);

    void IMigrator.Migrate()
    {
        _database.DropCollection("TimeTriggerEntity");
    }
}
