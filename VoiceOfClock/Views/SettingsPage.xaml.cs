using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
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
using VoiceOfClock.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();

        DataContext = _vm = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
    }

    private readonly SettingsPageViewModel _vm;

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

    [RelayCommand]
    Task OpenUri(string uri)
    {
        return Windows.System.Launcher.LaunchUriAsync(new Uri(uri)).AsTask();
    }

    // ms-settings uri scheme : https://learn.microsoft.com/en-us/windows/uwp/launch-resume/launch-settings-app
    private void OpenTimeSettings(object sender, RoutedEventArgs e)
    {
        _ = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:dateandtime"));
    }

    private void OpenSoundSettings(object sender, RoutedEventArgs e)
    {
        _ = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:sound"));
    }
}

public sealed class SettingContentDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SettingHeader { get; set; }
    public DataTemplate? ExpanderSettingContent { get; set; }
    public DataTemplate? SettingContentWithHeader { get; set; }
    public DataTemplate? SliderSettingContent { get; set; }
    public DataTemplate? ButtonSettingContent { get; set; }
    public DataTemplate? ComboBoxSettingContent { get; set; }
    public DataTemplate? ToggleSwitchSettingContent { get; set; }
    public DataTemplate? TextSettingContent { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return this.SelectTemplateCore(item, null!);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            ViewModels.SettingHeader => SettingHeader ?? base.SelectTemplateCore(item, container),
            ViewModels.ExpanderSettingContent => ExpanderSettingContent ?? base.SelectTemplateCore(item, container),
            ViewModels.SettingContentWithHeader => SettingContentWithHeader ?? base.SelectTemplateCore(item, container),
            ViewModels.SliderSettingContent => SliderSettingContent ?? base.SelectTemplateCore(item, container),
            ViewModels.ButtonSettingContent => ButtonSettingContent ?? base.SelectTemplateCore(item, container),
            ViewModels.ComboBoxSettingContent => ComboBoxSettingContent ?? base.SelectTemplateCore(item, container),
            ViewModels.ToggleSwitchSettingContent => ToggleSwitchSettingContent ?? base.SelectTemplateCore(item, container),
            ViewModels.TextSettingContent => TextSettingContent ?? base.SelectTemplateCore(item, container),
            _ => base.SelectTemplateCore(item, container),
        };
    }
}

public sealed class SettingsContentStyleSelector : StyleSelector
{
    public Style? Subheader { get; set; }
    public Style? Normal { get; set; }
    public Style? Expander { get; set; }
    public Style? ExpanderMiddle { get; set; }
    public Style? ExpanderLast { get; set; }

    protected override Style SelectStyleCore(object item, DependencyObject container)
    {
        if (item is SettingHeader && Subheader is not null)
        {
            return Subheader;
        }
        if (item is ExpanderSettingContent && Expander is not null)
        {
            return Expander;
        }
        else if (item is SettingContentWithHeader header)
        {
            return header.Position switch
            {
                SettingContainerPositionType.Normal => Normal ?? base.SelectStyleCore(item, container),
                SettingContainerPositionType.ExpanderMiddle => ExpanderMiddle ?? base.SelectStyleCore(item, container),
                SettingContainerPositionType.ExpanderLast => ExpanderLast ?? base.SelectStyleCore(item, container),
                _ => throw new NotSupportedException(),
            };
        }

        return base.SelectStyleCore(item, container);
    }
}
