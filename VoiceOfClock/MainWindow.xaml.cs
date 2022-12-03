using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI.Helpers;
using DryIoc;
using I18NPortable;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
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
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Services;
using VoiceOfClock.UseCases;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : SystemBackdropWindow
{
    public StoreLisenceService StoreLisenceService { get; }

    private readonly AppWindow _appWindow;

    public MainWindow()
    {
        this.InitializeComponent();        
        (this.Content as FrameworkElement)!.RequestedTheme = Ioc.Default.GetRequiredService<ApplicationSettings>().Theme switch 
        {
            Models.Domain.ApplicationTheme.Default => ElementTheme.Default,
            Models.Domain.ApplicationTheme.Dark => ElementTheme.Dark,
            Models.Domain.ApplicationTheme.Light => ElementTheme.Light,
        };
        TextBlock_AppTitle.Text = SystemInformation.Instance.ApplicationName;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBarDraggableArea);

        Title = SystemInformation.Instance.ApplicationName;

        if (TrySetSystemBackdrop() is false)
        {
            
        }

        StoreLisenceService = Ioc.Default.GetRequiredService<StoreLisenceService>();
        if (PurchaseItemsConstants.IsTrialLimitationEnabled)
        {
            StoreLisenceService.IsTrial.Throttle(TimeSpan.FromSeconds(1)).Subscribe(x =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    NVI_PurchaceLisence.Visibility = x ? Visibility.Visible : Visibility.Collapsed;
                });
            });
        }
        else
        {
            NVI_PurchaceLisence.Visibility = Visibility.Collapsed;
        }

        _ = StoreLisenceService.EnsureInitializeAsync();

        _appWindow = GetCurrentAppWindow();
        _appWindow.Closing += AppWindow_Closing;
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!SomeTimerIsActive())
        {
            return;
        }

        var appSettings = Ioc.Default.GetRequiredService<ApplicationSettings>();
                
        if (appSettings.DontShowWindowCloseConfirmDialog) { return; }

        args.Cancel = true;

        _ = DispatcherQueue.EnqueueAsync(async () => 
        {
            CheckBox_DontShowAgain.IsChecked = false;
            var result = await ContentDialog_ConfirmClosing.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                appSettings.DontShowWindowCloseConfirmDialog = CheckBox_DontShowAgain.IsChecked ?? false;
                this.Close();
            }
        });
    }

    static private bool SomeTimerIsActive()
    {
        return Ioc.Default.GetRequiredService<IMessenger>().Send<ActiveTimerCollectionRequestMessage>().Responses.Any();
    }

    public AppWindow GetCurrentAppWindow()
    {
        //Windowのハンドルを取得する
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        //hwndでWindowIdを取得する
        WindowId winId = Win32Interop.GetWindowIdFromWindow(hwnd);
        //WindowIdでAppWindow objectを取得して返す
        return AppWindow.GetFromWindowId(winId);
    }

    #region Backdrop


    #endregion
    public bool IsPageLoaded => ContentFrame.Content != null;


    public void NavigateFirstPage()
    {
        Navigate(typeof(Views.AlarmTimerPage), null);
    }

    public void Navigate(Type pageType, object? argument)
    {
        ContentFrame.Navigate(pageType, argument);
    }

    private void NVI_Alerm_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.AlarmTimerPage), null);
    }

    private void NVI_OneShotTimer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.OneShotTimerPage), null);
    }

    private void NVI_PeriodicTimer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.PeriodicTimerPage), null);
    }


    private void NVI_Settings_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.SettingsPage), null);
    }


    private void MyNavigationView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        if (args.DisplayMode == NavigationViewDisplayMode.Expanded)
        {
            //Button_ToggleNavigationMenu.Visibility = Visibility.Collapsed;
            MyNavigationView.IsPaneToggleButtonVisible = false;
            AppTitleBarDraggableArea.Margin = new Thickness(16, 0, 0, 0);
        }
        else
        {
            //Button_ToggleNavigationMenu.Visibility = Visibility.Visible;
            MyNavigationView.IsPaneToggleButtonVisible = true;
            AppTitleBarDraggableArea.Margin = new Thickness(60, 0, 0, 0);
        }
    }


    private void Button_ToggleNavigationMenu_Click(object sender, RoutedEventArgs e)
    {
        MyNavigationView.IsPaneOpen = !MyNavigationView.IsPaneOpen;
    }

    private async void NVI_PurchaceLisence_Tapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {            
            var (isSuccessed, error) = await StoreLisenceService.RequestPurchaiceLisenceAsync("PurchaseDialog_TitleOnInteractFromUser".Translate());
        }
        catch
        {

        }
    }

    private void NVI_AudioSoundSource_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.AudioSoundSourcePage), null);
    }
}
