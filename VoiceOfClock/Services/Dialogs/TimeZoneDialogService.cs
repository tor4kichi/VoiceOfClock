using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Views;

namespace VoiceOfClock.Services.Dialogs;

public sealed class TimeZoneDialogService : ITimeZoneDialogService
{
    public async Task<TimeZoneInfo?> ChoiceSingleTimeZoneAsync(Predicate<TimeZoneInfo> predicate)
    {
        SettingsTimeZoneSelectDialog dialog = new SettingsTimeZoneSelectDialog(TimeZoneInfo.GetSystemTimeZones().Where(x => predicate(x)));
        App.Current.InitializeDialog(dialog);

        if (await dialog.ShowAsync() == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            return dialog.SelectedTimeZoneInfo;
        }
        else
        {
            return null;
        }
    }
}
