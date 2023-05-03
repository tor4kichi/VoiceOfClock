using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading;
using VoiceOfClock.Core.Models;
using VoiceOfClock.ViewModels;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Views.Controls;

namespace VoiceOfClock.Views;

public sealed partial class OneShotTimerEditDialog : ContentDialog
{
    public OneShotTimerEditDialog()
    {
        this.InitializeComponent();

        Loaded += OneShotTimerEditDialog_Loaded;
        Closing += OneShotTimerEditDialog_Closing;

        _soundContentPlayerService = Ioc.Default.GetRequiredService<ISoundContentPlayerService>();
        _soundSelectionItems = _soundContentPlayerService
            .GetAllSoundContents()
            .Select(x => new SoundSelectionItemViewModel { SoundSourceType = x.SoundSourceType, Label = x.Label, SoundContent = x.SoundParameter })
            .ToArray();
    }

    private void OneShotTimerEditDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        TryCencelTestPlaySound();
    }

    private SoundSelectionItemViewModel? _firstSelectedsoundSelectionItem;
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly SoundSelectionItemViewModel[] _soundSelectionItems;
       

    public TimeSpan Duration
    {
        get => TimeSelectBox_Time.Time;
        set => TimeSelectBox_Time.Time = value;
    }

    public string TimerTitle
    {
        get => TextBox_EditTitle.Text;
        set => TextBox_EditTitle.Text = value;
    }


    public void SetSoundSource(SoundSourceType soundSourceType, string parameter)
    {
        if (soundSourceType is SoundSourceType.Tts or SoundSourceType.TtsWithSSML)
        {
            var selected = _soundSelectionItems.FirstOrDefault(x => x.SoundSourceType == soundSourceType) ?? _soundSelectionItems.First();
            ComboBox_SoundSelectionItem.SelectedItem = selected;
            _firstSelectedsoundSelectionItem = selected;
            TextBox_Tts.Text = parameter;
            _firstSelectedsoundSelectionItem!.SoundContent = parameter;
        }
        else
        {
            var selected = _soundSelectionItems.FirstOrDefault(x => x.SoundSourceType == soundSourceType && x.SoundContent == parameter) ?? _soundSelectionItems.First();
            ComboBox_SoundSelectionItem.SelectedItem = selected;
            _firstSelectedsoundSelectionItem = selected;
        }
    }

    public (SoundSourceType SoundSourceType, string SoundContent) GetSoundParameter()
    {
        var item = ((SoundSelectionItemViewModel)ComboBox_SoundSelectionItem.SelectedItem);
        return (item.SoundSourceType, item.SoundContent);
    }


    internal async Task<OneShotTimerDialogResult> ShowAsync(
        string dialogTitle, 
        string timerTitle, 
        TimeSpan time, 
        SoundSourceType soundSourceType, 
        string soundParameter,
        TimeSelectBoxDisplayMode timeSelectBoxDisplayMode
        )
    {
        Title = dialogTitle;
        TimerTitle = timerTitle;
        Duration = time;
        TimeSelectBox_Time.DisplayMode = timeSelectBoxDisplayMode;
        SetSoundSource(soundSourceType, soundParameter);
        if (await base.ShowAsync() is ContentDialogResult.Primary)
        {
            var (resultSoundSourceType, resultParameter) = GetSoundParameter();
            return new OneShotTimerDialogResult()
            {
                IsConfirmed = true,
                Time = Duration,
                Title = TimerTitle,
                SoundSourceType = resultSoundSourceType,
                SoundParameter = resultParameter
            };
        }
        else
        {
            return new OneShotTimerDialogResult { IsConfirmed = false };
        }
    }


    private void OneShotTimerEditDialog_Loaded(object sender, RoutedEventArgs e)
    {
        ComboBox_SoundSelectionItem.SelectedItem = _firstSelectedsoundSelectionItem;
        UpdateTextBoxTtsVisibility();
    }



    private void TextBox_SoundParameter_TextChanged(object sender, TextChangedEventArgs e)
    {
        var item = (SoundSelectionItemViewModel)ComboBox_SoundSelectionItem.SelectedItem;
        if (item.SoundSourceType is SoundSourceType.Tts or SoundSourceType.TtsWithSSML)
        {
            item.SoundContent = TextBox_Tts.Text;
        }

        UpdateIsPrimaryButtonEnabled();
    }


    private void UpdateIsPrimaryButtonEnabled()
    {
        var (type, parameter) = GetSoundParameter();
        bool isValid = false;
        if (type == SoundSourceType.System)
        {
            isValid = IsValidTime(Duration);
        }
        else if (type == SoundSourceType.Tts)
        {
            isValid = IsValidInput(parameter) && IsValidTime(Duration);
        }
        else if (type == SoundSourceType.TtsWithSSML)
        {
            isValid = IsValidInput(parameter) && IsValidTime(Duration);
        }
        else if (type == SoundSourceType.AudioFile)
        {
            isValid = IsValidTime(Duration);
        }

        IsPrimaryButtonEnabled = isValid;
        Button_TestPlaySound.IsEnabled = isValid;
    }

    static bool IsValidInput(string text)
    {
        return !string.IsNullOrWhiteSpace(text);
    }

    static bool IsValidTime(TimeSpan time)
    {
        return time > TimeSpan.Zero;
    }


    CancellationTokenSource? _testPlaySoundCanceller;
    
    [RelayCommand]
    async Task TestPlaySound()
    {
        if (_testPlaySoundCanceller is not null)
        {
            _testPlaySoundCanceller.Cancel();
            _testPlaySoundCanceller.Dispose();
        }

        _testPlaySoundCanceller = new CancellationTokenSource();
        CancellationTokenSource cts = _testPlaySoundCanceller;
        CancellationToken ct = cts.Token;
        
        var (soundSourceType, soundContent) = GetSoundParameter();
        await _soundContentPlayerService.PlaySoundContentAsync(soundSourceType, soundContent, ct);
    }

    void TryCencelTestPlaySound()
    {
        if (_testPlaySoundCanceller is not null)
        {
            _testPlaySoundCanceller.Cancel();
            _testPlaySoundCanceller.Dispose();
            _testPlaySoundCanceller = null;
        }
    }

    private void TimeSelectBox_Time_TimeChanged(Controls.TimeSelectBox sender, Controls.TimeSelectBoxTimeValueChangedEventArgs args)
    {
        UpdateIsPrimaryButtonEnabled();
    }

    private void ComboBox_SoundSelectionItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateIsPrimaryButtonEnabled();
        UpdateTextBoxTtsVisibility();
    }

    private void UpdateTextBoxTtsVisibility()
    {
        if (ComboBox_SoundSelectionItem.SelectedItem is SoundSelectionItemViewModel item
            && item.SoundSourceType is SoundSourceType.Tts or SoundSourceType.TtsWithSSML
            )
        {
            ContentWithIcon_Tts.Visibility = Visibility.Visible;
        }
        else
        {
            ContentWithIcon_Tts.Visibility = Visibility.Collapsed;
        }
    }

    
}
