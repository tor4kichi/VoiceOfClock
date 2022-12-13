using VoiceOfClock.Core.Models;

namespace VoiceOfClock.Core.Contracts.Models;

public interface ITimer
{
    string Title { get; }
    SoundSourceType SoundSourceType { get; }
    string SoundContent { get; }
}