using System;
using System.Threading.Tasks;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.Contracts.Services;

namespace VoiceOfClock.Services.Dialogs;

public sealed class OneShotTimerEditDialogService : IOneShotTimerDialogService
{
    public async Task<OneShotTimerDialogResult> ShowEditTimerAsync(
        string dialogTitle
        , string timerTitle
        , TimeSpan time
        , SoundSourceType soundSourceType
        , string soundParameter
        )
    {
        var dialog = new Views.OneShotTimerEditDialog();
        App.Current.InitializeDialog(dialog);
        return await dialog.ShowAsync(dialogTitle, timerTitle, time, soundSourceType, soundParameter);
    }
}
