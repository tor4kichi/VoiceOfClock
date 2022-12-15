namespace VoiceOfClock.Core.Infrastructure;

public interface IMigrator
{
    Version TargetVersion { get; }
    void Migrate();
}
