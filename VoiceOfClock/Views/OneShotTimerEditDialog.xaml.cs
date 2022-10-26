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
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceOfClock.UseCases;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using DependencyPropertyGenerator;

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
        var dialog = new OneShotTimerEditDialog()
        {
            Title = dialogTitle,
            TimerTitle = timerTitle,
            Duration = time,
            //SoundSourceType = soundSourceType,
        };
        dialog.SetSoundSource(soundSourceType, soundParameter);

        App.Current.InitializeDialog(dialog);

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            return new OneShotTimerDialogResult()
            {
                IsConfirmed = true,
                Time = dialog.Duration,
                Title = dialog.TimerTitle,
                SoundSourceType = dialog.SoundSourceType,
                SoundParameter = dialog.GetSoundParameter()
            };
        }
        else
        {
            return new OneShotTimerDialogResult { IsConfirmed = false };
        }
    }
}

public sealed partial class SoundSourceViewModel : ObservableObject
{
    public SoundSourceViewModel(SoundSourceType soundSourceType, string parameter)
    {
        SoundSourceType = soundSourceType;
        Parameter = parameter;
    }

    public SoundSourceType SoundSourceType { get; }
    public string Parameter { get; }
}

public sealed partial class OneShotTimerEditDialog : ContentDialog
{
    public OneShotTimerEditDialog()
    {
        this.InitializeComponent();

        Loaded += OneShotTimerEditDialog_Loaded;
    }

    private void OneShotTimerEditDialog_Loaded(object sender, RoutedEventArgs e)
    {
        ComboBox_SoundSourceType.SelectedItem = SoundSourceType;
        ComboBox_SoundSource_WindowsSystem.SelectedItem = _SystemSoundSource_InitParameter ?? _windowsSystemParameters[0];
    }

    private SoundSourceType[] _soundSourceTypes = new[] 
    {
        SoundSourceType.System,
        SoundSourceType.Tts,
        //SoundSourceType.TtsWithSSML,
    };

    private string[] _windowsSystemParameters = Enum.GetNames<WindowsNotificationSoundType>();

    public TimeSpan Duration
    {
        get => TimeSelector_Time.Time;
        set => TimeSelector_Time.Time = value;
    }

    public string TimerTitle
    {
        get => TextBox_EditTitle.Text;
        set => TextBox_EditTitle.Text = value;
    }

    public SoundSourceType SoundSourceType { get; set; }

    private string _SystemSoundSource_InitParameter;

    public void SetSoundSource(SoundSourceType soundSourceType, string parameter)
    {
        SoundSourceType = soundSourceType;
        ComboBox_SoundSourceType.SelectedItem = soundSourceType;
        if (soundSourceType == SoundSourceType.System)
        {
            _SystemSoundSource_InitParameter = _windowsSystemParameters.FirstOrDefault(x => x == parameter);
            ComboBox_SoundSource_WindowsSystem.SelectedItem = _SystemSoundSource_InitParameter;
            TextBox_SoundParameter_Tts.Text = String.Empty;
            TextBox_SoundParameter_TtsWithSsml.Text = String.Empty;
        }
        else if (soundSourceType == SoundSourceType.Tts)
        {
            ComboBox_SoundSource_WindowsSystem.SelectedItem = SystemSoundConstants.Default;
            TextBox_SoundParameter_Tts.Text = parameter;
            TextBox_SoundParameter_TtsWithSsml.Text = String.Empty;
        }
        else if (soundSourceType == SoundSourceType.TtsWithSSML)
        {
            ComboBox_SoundSource_WindowsSystem.SelectedItem = SystemSoundConstants.Default;
            TextBox_SoundParameter_Tts.Text = String.Empty;
            TextBox_SoundParameter_TtsWithSsml.Text = parameter;
        }
    }

    public string GetSoundParameter()
    {
        return SoundSourceType switch
        {
            SoundSourceType.System => (string)ComboBox_SoundSource_WindowsSystem.SelectedItem,
            SoundSourceType.Tts => TextBox_SoundParameter_Tts.Text,
            SoundSourceType.TtsWithSSML => TextBox_SoundParameter_TtsWithSsml.Text,
            _ => throw new NotSupportedException(SoundSourceType.ToString()),
        };
    }

    private void ComboBox_SoundSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TextBox_SoundParameter_Tts.TextChanged -= TextBox_SoundParameter_TextChanged;
        TextBox_SoundParameter_TtsWithSsml.TextChanged -= TextBox_SoundParameter_TextChanged;

        var type = (SoundSourceType)ComboBox_SoundSourceType.SelectedItem;
        SoundSourceType = type;
        if (type == SoundSourceType.System)
        {
            ComboBox_SoundSource_WindowsSystem.Visibility = Visibility.Visible;
            TextBox_SoundParameter_Tts.Visibility = Visibility.Collapsed;
            TextBox_SoundParameter_TtsWithSsml.Visibility = Visibility.Collapsed;
        }
        else if (type == SoundSourceType.Tts)
        {
            ComboBox_SoundSource_WindowsSystem.Visibility = Visibility.Collapsed;
            TextBox_SoundParameter_Tts.Visibility = Visibility.Visible;
            TextBox_SoundParameter_TtsWithSsml.Visibility = Visibility.Collapsed;

            TextBox_SoundParameter_Tts.TextChanged += TextBox_SoundParameter_TextChanged;
        }
        else if (type == SoundSourceType.TtsWithSSML)
        {
            ComboBox_SoundSource_WindowsSystem.Visibility = Visibility.Collapsed;
            TextBox_SoundParameter_Tts.Visibility = Visibility.Collapsed;
            TextBox_SoundParameter_TtsWithSsml.Visibility = Visibility.Visible;

            TextBox_SoundParameter_TtsWithSsml.TextChanged += TextBox_SoundParameter_TextChanged;
        }

        UpdateIsPrimaryButtonEnabled();
    }

    private void TextBox_SoundParameter_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateIsPrimaryButtonEnabled();
    }


    private void UpdateIsPrimaryButtonEnabled()
    {
        var type = SoundSourceType;
        if (type == SoundSourceType.System)
        {
            IsPrimaryButtonEnabled = IsValidTime(Duration);
        }
        else if (type == SoundSourceType.Tts)
        {
            IsPrimaryButtonEnabled = IsValidInput(TextBox_SoundParameter_Tts.Text) && IsValidTime(Duration);
        }
        else if (type == SoundSourceType.TtsWithSSML)
        {
            IsPrimaryButtonEnabled = IsValidInput(TextBox_SoundParameter_TtsWithSsml.Text) && IsValidTime(Duration);
        }
    }

    private void TimePicker_Time_SelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
    {
        UpdateIsPrimaryButtonEnabled();
    }

    static bool IsValidInput(string text)
    {
        return !string.IsNullOrWhiteSpace(text);
    }

    static bool IsValidTime(TimeSpan time)
    {
        return time > TimeSpan.Zero;
    }

    [RelayCommand]
    async Task TestPlaySound()
    {
        var meseenger = Ioc.Default.GetRequiredService<IMessenger>();
        if (SoundSourceType == SoundSourceType.System)
        {
            var notificationSoundType = Enum.Parse<WindowsNotificationSoundType>((string)ComboBox_SoundSource_WindowsSystem.SelectedItem);
            _ = meseenger.Send(new PlaySystemSoundRequest(notificationSoundType));
        }
        else if (SoundSourceType == SoundSourceType.Tts)
        {
            await meseenger.Send(new TextPlayVoiceRequest(TextBox_SoundParameter_Tts.Text));
        }
        else if (SoundSourceType == SoundSourceType.TtsWithSSML)
        {
            await meseenger.Send(new SsmlPlayVoiceRequest(TextBox_SoundParameter_TtsWithSsml.Text));
        }
    }

    bool _skipOnFirst = true;
    private void ComboBox_SoundSource_WindowsSystem_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_skipOnFirst)
        {
            _skipOnFirst = false;
            return;
        }
        
        _ = TestPlaySound();
    }

}


public sealed class SoundSourceDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate WindowsSystem { get; set; }
    public DataTemplate Tts { get; set; }
    public DataTemplate TtsWithSsml { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return this.SelectTemplateCore(item, null);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return (SoundSourceType)item switch
        {
            SoundSourceType.System => WindowsSystem,
            SoundSourceType.Tts => Tts,
            SoundSourceType.TtsWithSSML => TtsWithSsml,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}