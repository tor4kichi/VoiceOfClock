using System;
using System.Threading.Tasks;
using VoiceOfClock.Core.Models;
using Windows.Storage;

namespace VoiceOfClock.Contracts.Services;

public interface IAudioSoundSourceDialogService
{
    Task<AudioSoundSourceDialogResult> ShowAsync(string dialogTitle, string filePath, TimeSpan duration, TimeSpan start, TimeSpan end, string title, double soundVolume);

    Task<StorageFile> ChoiceFileAsync(string dialogTitle);
}

public sealed class AudioSoundSourceDialogResult
{
    public static readonly AudioSoundSourceDialogResult Failed = new AudioSoundSourceDialogResult() { IsConfirmed = false };

    public static AudioSoundSourceDialogResult Success(string filePath, TimeSpan duration, TimeSpan start, TimeSpan end, string title, double soundVolume)
    {
        return new AudioSoundSourceDialogResult()
        {
            IsConfirmed = true
            ,
            FilePath = filePath
            ,
            Duration = duration
            ,
            AudioSpan = new AudioSpan(start, end)
            ,
            Title = title
            ,
            SoundVolume = soundVolume
        };
    }

    private AudioSoundSourceDialogResult()
    {
        FilePath = string.Empty;
    }


    public bool IsConfirmed { get; init; }

    public string FilePath { get; init; }

    public TimeSpan Duration { get; init; }

    public AudioSpan AudioSpan { get; set; }

    public string? Title { get; set; }

    public double SoundVolume { get; set; } = 1.0;
}
