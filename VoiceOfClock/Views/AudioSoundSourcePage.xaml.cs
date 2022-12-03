﻿using CommunityToolkit.Mvvm.DependencyInjection;
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
using VoiceOfClock.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace VoiceOfClock.Views;

public sealed partial class AudioSoundSourcePage : Page
{
    public AudioSoundSourcePage()
    {
        this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<AudioSoundSourcePageViewModel>();
    }

    private readonly AudioSoundSourcePageViewModel _vm;

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
    private void MenuFlyout_Opening(object sender, object e)
    {
        var focusItem = (sender as FlyoutBase)!.Target as ContentControl ?? throw new NullReferenceException();
        if (sender is MenuFlyout menuFlyout)
        {
            foreach (var item in menuFlyout.Items)
            {
                item.DataContext = focusItem.Content;
            }
        }
    }

    private void MenuFlyoutItem_DeleteItem_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }
}
