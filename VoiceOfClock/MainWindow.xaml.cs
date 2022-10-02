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
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
    }


    public bool IsPageLoaded => ContentFrame.Content != null;


    public void NavigateFirstPage(string argument)
    {
        Navigate(typeof(Views.PeriodicTimerPage), null);
    }

    public void Navigate(Type pageType, object argument)
    {
        ContentFrame.Navigate(pageType, argument);
    }

    private void NVI_Alerm_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.AlermPage), null);
    }

    private void NVI_Timer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.TimerPage), null);
    }

    private void NVI_PeriodicTimer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        Navigate(typeof(Views.PeriodicTimerPage), null);
    }
}
