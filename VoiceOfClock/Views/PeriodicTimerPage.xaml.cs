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
using VoiceOfClock.Models.Domain;
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
    private readonly PeriodicTimerRepository _periodicTimerRepository;
    private readonly IMessenger _messenger;
    private readonly ObservableCollection<PeriodicTimerViewModel> _timers;

    public PeriodicTimerPage()
    {
        this.InitializeComponent();

        _periodicTimerRepository =  Ioc.Default.GetRequiredService<PeriodicTimerRepository>();
        _messenger = Ioc.Default.GetRequiredService<IMessenger>();
        _timers = new ObservableCollection<PeriodicTimerViewModel>(_periodicTimerRepository.ReadAllItems().Select(x => new PeriodicTimerViewModel(x, _periodicTimerRepository, _messenger)));
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        foreach (var itemVM in _timers)
        {
            _messenger.RegisterAll(itemVM);
        }

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        foreach (var itemVM in _timers)
        {
            _messenger.UnregisterAll(itemVM);
        }

        base.OnNavigatingFrom(e);
    }

    bool _isEditting;

    private void SymbolIcon_Edit_Tapped(object sender, TappedRoutedEventArgs e)
    {
        _isEditting = !_isEditting;
        SymbolIcon_Edit.Symbol = _isEditting ? Symbol.Edit : Symbol.Accept;
        foreach (var timerVM in _timers)
        {
            timerVM.IsEditting = _isEditting;
        }
    }

    bool _nowEditting;

    private async void Button_AddTimer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_nowEditting) { return; }

        _nowEditting = true;
        try
        {
            Grid_EditTimer.Visibility = Visibility.Visible;
            ContentDialog_EditTimer.Title = "タイマーを追加";
            TextBox_EditTitle.Text = string.Empty;
            TimePicker_IntervalTime.SelectedTime = TimeSpan.FromMinutes(5);
            TimePicker_StartTime.SelectedTime = TimeSpan.Zero;
            TimePicker_EndTime.SelectedTime = TimeSpan.FromHours(1);
            var result = await ContentDialog_EditTimer.ShowAsync();                
            if (result == ContentDialogResult.Primary)
            {
                Guard.IsNotNull(TimePicker_IntervalTime.SelectedTime, nameof(TimePicker_IntervalTime.SelectedTime));
                Guard.IsNotNull(TimePicker_StartTime.SelectedTime, nameof(TimePicker_StartTime.SelectedTime));
                Guard.IsNotNull(TimePicker_EndTime.SelectedTime, nameof(TimePicker_EndTime.SelectedTime));


                var newEntity = new PeriodicTimerEntity()
                {
                    IsEnabled = true,
                    Title = TextBox_EditTitle.Text,
                    IntervalTime = TimePicker_IntervalTime.SelectedTime.Value,
                    StartTime = TimePicker_StartTime.SelectedTime.Value,
                    EndTime = TimePicker_EndTime.SelectedTime.Value,
                };

                _periodicTimerRepository.CreateItem(newEntity);
                var itemVM = new PeriodicTimerViewModel(newEntity, _periodicTimerRepository, _messenger);
                _timers.Add(itemVM);
                _messenger.RegisterAll(itemVM);
            }
        }
        finally
        {
            _nowEditting = false;
        }
    }

    private void Grid_EditTimer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.OriginalSource == Grid_EditTimer)
        {
            Grid_EditTimer.Visibility = Visibility.Collapsed;
            ContentDialog_EditTimer.Hide();
        }
    }
 
    private void ContentDialog_EditTimer_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Grid_EditTimer.Visibility = Visibility.Collapsed;
    }

    private void ContentDialog_EditTimer_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Grid_EditTimer.Visibility = Visibility.Collapsed;
    }


    [RelayCommand]
    private async Task TimerEdit(PeriodicTimerViewModel timerVM)
    {
        Grid_EditTimer.Visibility = Visibility.Visible;
        ContentDialog_EditTimer.Title = "タイマーを編集";
        TextBox_EditTitle.Text = timerVM.Title ?? string.Empty;
        TimePicker_IntervalTime.SelectedTime = timerVM.IntervalTime;
        TimePicker_StartTime.SelectedTime = timerVM.StartTime;
        TimePicker_EndTime.SelectedTime = timerVM.EndTime;

        var result = await ContentDialog_EditTimer.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            Guard.IsNotNull(TimePicker_IntervalTime.SelectedTime, nameof(TimePicker_IntervalTime.SelectedTime));
            Guard.IsNotNull(TimePicker_StartTime.SelectedTime, nameof(TimePicker_StartTime.SelectedTime));
            Guard.IsNotNull(TimePicker_EndTime.SelectedTime, nameof(TimePicker_EndTime.SelectedTime));

            timerVM.Title = TextBox_EditTitle.Text;
            timerVM.IntervalTime = TimePicker_IntervalTime.SelectedTime.Value;
            timerVM.StartTime = TimePicker_StartTime.SelectedTime.Value;
            timerVM.EndTime = TimePicker_EndTime.SelectedTime.Value;
            timerVM.UpdateEntity();
        }
    }

    private void AdaptiveGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        _ = TimerEdit(e.ClickedItem as PeriodicTimerViewModel);
    }

    PeriodicTimerEntity _tempPeriodicTimer;
    private void Button_ImmidiateStart_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_tempPeriodicTimer != null) { return; }

        TimeSpan timeOfDay = DateTime.Now.TimeOfDay;
        timeOfDay = new TimeSpan(timeOfDay.Hours, timeOfDay.Minutes, 0);
        // 永続化せずに実行できるようにしたい
        _tempPeriodicTimer = new PeriodicTimerEntity()
        {
            Id = Guid.NewGuid(),
            IsEnabled = true,
            IntervalTime = TimePicker_ImmidiateRunTimer.SelectedTime.Value,
            StartTime = timeOfDay,
            EndTime = timeOfDay - TimeSpan.FromSeconds(1),
        };

        _messenger.Send(new PeriodicTimerAdded(_tempPeriodicTimer));

        TimePicker_ImmidiateRunTimer.IsEnabled = false;
        Button_ImmidiateStart.IsEnabled = false;
        Button_ImmidiateStart.Visibility = Visibility.Collapsed;
        Button_ImmidiateStop.IsEnabled = true;
        Button_ImmidiateStop.Visibility = Visibility.Visible;
    }

    private void Button_ImmidiateStop_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_tempPeriodicTimer == null) { return; }
        
        _messenger.Send(new PeriodicTimerRemoved(_tempPeriodicTimer));
        _tempPeriodicTimer = null;

        TimePicker_ImmidiateRunTimer.IsEnabled = true;
        Button_ImmidiateStart.IsEnabled = true;
        Button_ImmidiateStart.Visibility = Visibility.Visible;
        Button_ImmidiateStop.IsEnabled = false;
        Button_ImmidiateStop.Visibility = Visibility.Collapsed;
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

    private void MenuFlyoutItem_TimerEdit_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe && fe.DataContext is PeriodicTimerViewModel timerVM)
        {
            _ = TimerEdit(timerVM);
        }
    }


    private void MenuFlyoutItem_TimerDelete_Click(object sender, RoutedEventArgs e)
    {
        // _messenger.UnregisterAll(itemVM);
    }
}    
