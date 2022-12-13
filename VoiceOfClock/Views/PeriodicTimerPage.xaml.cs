using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.UseCases;
using VoiceOfClock.ViewModels;
using Microsoft.UI.Xaml.Controls.Primitives;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace VoiceOfClock.Views;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class PeriodicTimerPage : Page
{
    private readonly PeriodicTimerPageViewModel _vm;
    public PeriodicTimerPage()
    {
        this.InitializeComponent();

        DataContext = _vm = Ioc.Default.GetRequiredService<PeriodicTimerPageViewModel>();
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _vm.IsActive = true;

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        _vm.IsActive = false;

        base.OnNavigatingFrom(e);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:メンバーを static に設定します", Justification = "<保留中>")]
    private void MenuFlyout_TimerItem_Opening(object sender, object e)
    {        
        ContentControl focusItem = (sender as FlyoutBase)!.Target as ContentControl ?? throw new NullReferenceException();
        if (sender is MenuFlyout menuFlyout)
        {
            foreach (var item in menuFlyout.Items)
            {
                item.DataContext = focusItem.Content;
            }
        }
    }
}    
