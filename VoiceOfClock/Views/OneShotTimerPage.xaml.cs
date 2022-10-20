using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using VoiceOfClock.ViewModels;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace VoiceOfClock.Views;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class OneShotTimerPage : Page
{
    public OneShotTimerPage()
    {
        this.InitializeComponent();

        DataContext = _vm = Ioc.Default.GetRequiredService<OneShotTimerPageViewModel>();
    }

    private readonly OneShotTimerPageViewModel _vm;

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

    private void MenuFlyout_TimerItem_Opening(object sender, object e)
    {
        var focusItem = (sender as FlyoutBase).Target as ContentControl;
        if (sender is MenuFlyout menuFlyout)
        {
            foreach (var item in menuFlyout.Items)
            {
                item.DataContext = focusItem.Content;
            }
        }
    }
}
