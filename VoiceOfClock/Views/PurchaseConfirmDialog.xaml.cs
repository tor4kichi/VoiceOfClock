using I18NPortable;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VoiceOfClock.Contract.Services;
using VoiceOfClock.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock.Views;


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

public sealed partial class PurchaseConfirmDialog : ContentDialog
{
    public PurchaseConfirmDialog()
    {
        this.InitializeComponent();

        CB_LisenceFeature_1.Content = "PurchaseDialog_LisenceFeature_1".Translate(PurchaseItemsConstants.Trial_TimersLimitationCount);               
    }
}
