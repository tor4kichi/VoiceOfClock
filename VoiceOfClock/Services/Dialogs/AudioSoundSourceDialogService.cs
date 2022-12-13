using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;

namespace VoiceOfClock.Services.Dialogs;

public sealed class AudioSoundSourceDialogService : IAudioSoundSourceDialogService
{
    async Task<AudioSoundSourceDialogResult> IAudioSoundSourceDialogService.ShowAsync(string dialogTitle, string filePath, TimeSpan duration, TimeSpan start, TimeSpan end, string title, double soundVolume)
    {
        var dialog = new Views.AudioSoundSourceEditDialog();
        App.Current.InitializeDialog(dialog);
        dialog.Title = dialogTitle;
        dialog.FilePath = filePath;
        dialog.Duration = duration;
        dialog.AudioBegin = start;
        dialog.AudioEnd = end;
        dialog.Label= title;
        dialog.SoundVolume = soundVolume;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            return AudioSoundSourceDialogResult.Success(
                dialog.FilePath!, 
                dialog.Duration,
                dialog.AudioBegin,
                dialog.AudioEnd,
                dialog.Label!,
                dialog.SoundVolume
                );
        }   
        else
        {
            return AudioSoundSourceDialogResult.Failed;
        }
    }
}
