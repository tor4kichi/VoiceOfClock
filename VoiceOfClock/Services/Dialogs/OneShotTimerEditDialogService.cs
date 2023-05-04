using System;
using System.Threading.Tasks;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Views.Controls;

namespace VoiceOfClock.Services.Dialogs;

public sealed class OneShotTimerEditDialogService : IOneShotTimerDialogService
{
    public async Task<OneShotTimerDialogResult> ShowEditTimerAsync(
        string dialogTitle
        , string timerTitle
        , TimeSpan time
        , SoundSourceType soundSourceType
        , string soundParameter
        , TimeSelectBoxDisplayMode timeSelectBoxDisplayMode 
        )
    {
        var dialog = new Views.OneShotTimerEditDialog();
        App.Current.InitializeDialog(dialog);
        return await dialog.ShowAsync(dialogTitle, timerTitle, time, soundSourceType, soundParameter, timeSelectBoxDisplayMode);
    }
}
