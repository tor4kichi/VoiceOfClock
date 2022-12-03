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
using VoiceOfClock.ViewModels;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using DependencyPropertyGenerator;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Threading;
using VoiceOfClock.Core.Domain;
using I18NPortable;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock.Views;

public sealed class OneShotTimerEditDialogService : IOneShotTimerDialogService
{
    public async Task<OneShotTimerDialogResult> ShowEditTimerAsync(
        string dialogTitle
        , string timerTitle
        , TimeSpan time
        , SoundSourceType soundSourceType
        , string soundParameter
        )
    {
        var dialog = new OneShotTimerEditDialog();
        App.Current.InitializeDialog(dialog);
        return await dialog.ShowAsync(dialogTitle, timerTitle, time, soundSourceType, soundParameter);
    }
}

public sealed partial class OneShotTimerEditDialog : ContentDialog
{
    public OneShotTimerEditDialog()
    {
        this.InitializeComponent();

        Loaded += OneShotTimerEditDialog_Loaded;
        Closing += OneShotTimerEditDialog_Closing;

        _soundSelectionItems = new[]
        {
            new [] { new SoundSelectionItemViewModel { SoundSourceType = SoundSourceType.Tts, Label = SoundSourceType.Tts.Translate() } },
            Ioc.Default.GetRequiredService<AudioSoundSourceRepository>().ReadAllItems().Select(x => new SoundSelectionItemViewModel {SoundSourceType = SoundSourceType.AudioFile,  SoundContent = x.Id.ToString(), Label = !string.IsNullOrWhiteSpace(x.Title) ? x.Title : Path.GetFileNameWithoutExtension(x.FilePath) }),
            Enum.GetNames<WindowsNotificationSoundType>().Select(x => new SoundSelectionItemViewModel { SoundSourceType = SoundSourceType.System, SoundContent = x, Label = x }),
        }
        .SelectMany(x => x)
        .ToArray();
    }

    private void OneShotTimerEditDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        TryCencelTestPlaySound();
    }

    private SoundSelectionItemViewModel? _firstSelectedsoundSelectionItem;

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


    internal async Task<OneShotTimerDialogResult> ShowAsync(string dialogTitle, string timerTitle, TimeSpan time, SoundSourceType soundSourceType, string soundParameter)
    {
        Title = dialogTitle;
        TimerTitle = timerTitle;
        Duration = time;
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
    void TestPlaySound()
    {
        if (_testPlaySoundCanceller is not null)
        {
            _testPlaySoundCanceller.Cancel();
            _testPlaySoundCanceller.Dispose();
        }

        _testPlaySoundCanceller = new CancellationTokenSource();
        CancellationTokenSource cts = _testPlaySoundCanceller;
        CancellationToken ct = cts.Token;
        
        var meseenger = Ioc.Default.GetRequiredService<IMessenger>();
        var (soundSourceType, soundContent) = GetSoundParameter();
        if (soundSourceType == SoundSourceType.System)
        {
            if (Enum.TryParse<WindowsNotificationSoundType>(soundContent, out var notificationSoundType))
            {
                _ = meseenger.Send(new PlaySystemSoundRequest(notificationSoundType, ct));
            }
        }
        else if (soundSourceType == SoundSourceType.Tts)
        {
            if (!string.IsNullOrWhiteSpace(soundContent))
            {
                _ = meseenger.Send(new TextPlayVoiceRequest(soundContent, ct));
            }
        }
        else if (soundSourceType == SoundSourceType.TtsWithSSML)
        {
            if (!string.IsNullOrWhiteSpace(soundContent))
            {
                _ = meseenger.Send(new SsmlPlayVoiceRequest(soundContent, ct));
            }
        }
        else if (soundSourceType == SoundSourceType.AudioFile)
        {
            _ = meseenger.Send(new PlayAudioRequestMessage(soundContent, ct));
        }
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
