#nullable enable
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace VoiceOfClock.Services.Dialogs;

public sealed class AudioSoundSourceDialogService : IAudioSoundSourceDialogService
{
    // supported filetypes 
    // https://support.microsoft.com/en-us/topic/file-types-supported-by-windows-media-player-32d9998e-dc8f-af54-7ba1-e996f74375d9        
    public static readonly string[] SupprotedAudioFileTypes = new[]
    {
        ".asf", ".wma", ".wmv", ".wm",
        ".mpg", ".mpeg", ".m1v", ".mp2", ".mp3", ".mpa", ".mpe", ".m3u",
        ".wav",
        ".mov",
        ".m4a",
        ".mp4", ".m4v", ".mp4v", ".3g2", ".3gp2", ".3gp", ".3gpp",
        ".aac", ".adt", ".adts",
        ".flac",
    };

    async Task<StorageFile?> IAudioSoundSourceDialogService.ChoiceFileAsync(string dialogTitle)
    {
        var fileOpenPicker = new FileOpenPicker();
        App.Current.InitializeWithWindow(fileOpenPicker);
        fileOpenPicker.ViewMode = PickerViewMode.List;
        fileOpenPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

        foreach (var fileType in SupprotedAudioFileTypes)
        {
            fileOpenPicker.FileTypeFilter.Add(fileType);
        }

        return await fileOpenPicker.PickSingleFileAsync();
    }

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
