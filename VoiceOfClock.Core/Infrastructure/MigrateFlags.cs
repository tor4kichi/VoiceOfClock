namespace VoiceOfClock.Core.Infrastructure;

public sealed class MigrateFlags : SettingsBase
{
    Dictionary<string, bool?> _flags = new Dictionary<string, bool?>();
    public bool? this[string flagName]
    {
        get
        {
            if (_flags.TryGetValue(flagName, out var flagValue))
            {
                return flagValue;
            }
            else
            {
                return _flags[flagName] = Read(default(bool?), flagName);
            }
        }
        set
        {
            SetProperty(_flags[flagName], value, (val) => _flags[flagName] = val, flagName);
        }
    }

    public void ExecuteIfNotMigrated(string flagName, Action migrateAction)
    {
        if (_flags[flagName] == null)
        {
            try
            {
                migrateAction();
                _flags[flagName] = true;
            }
            catch
            {
                _flags[flagName] = false;
                throw;
            }
        }
    }
}
