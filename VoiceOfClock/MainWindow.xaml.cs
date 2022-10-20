using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI.Helpers;
using DryIoc;
using Microsoft.UI.Composition.SystemBackdrops;
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
using VoiceOfClock.Models.Domain;
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
    public MainWindow()
    {
        this.InitializeComponent();        
        (this.Content as FrameworkElement).RequestedTheme = Ioc.Default.GetRequiredService<ApplicationSettings>().Theme;
        TextBlock_AppTitle.Text = SystemInformation.Instance.ApplicationName;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBarDraggableArea);

        Title = SystemInformation.Instance.ApplicationName;

        if (TrySetSystemBackdrop() is false)
        {
            
        }
    }

    #region Backdrop


    #endregion
    public bool IsPageLoaded => ContentFrame.Content != null;


    public void NavigateFirstPage(string argument)
    {
        Navigate(typeof(Views.OneShotTimerPage), null);
    }

    public void Navigate(Type pageType, object argument)
    {
        ContentFrame.Navigate(pageType, argument);
    }

    private void NVI_Alerm_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.AlermPage), null);
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
}
