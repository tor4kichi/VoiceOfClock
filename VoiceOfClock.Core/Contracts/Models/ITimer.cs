using VoiceOfClock.Core.Models;

namespace VoiceOfClock.Core.Contracts.Models;

public interface ITimer
{
    Guid Id { get; }
    string Title { get; }
    SoundSourceType SoundSourceType { get; }
    string SoundContent { get; }    
}