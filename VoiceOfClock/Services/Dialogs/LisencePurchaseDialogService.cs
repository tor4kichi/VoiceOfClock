using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Views;

namespace VoiceOfClock.Services.Dialogs;

public sealed class LisencePurchaseDialogService : ILisencePurchaseDialogService
{
    async Task<bool> ILisencePurchaseDialogService.ShowPurchaseMainProductConfirmDialogAsync(string dialogTitle)
    {
        var dialog = new PurchaseConfirmDialog();
        App.Current.InitializeDialog(dialog);
        dialog.Title = dialogTitle;
        return await dialog.ShowAsync() is ContentDialogResult.Primary;
    }
}
