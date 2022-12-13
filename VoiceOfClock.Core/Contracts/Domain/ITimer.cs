using VoiceOfClock.Core.Domain;

namespace VoiceOfClock.Core.Contracts.Domain;

public interface ITimer
{
    string Title { get; }
    SoundSourceType SoundSourceType { get; }
    string SoundContent { get; }
}