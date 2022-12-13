using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DependencyPropertyGenerator;
using I18NPortable;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;
using VoiceOfClock.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock.Views;

[DependencyProperty<bool>("IsRepeat")]
[DependencyProperty<EnabledDayOfWeekViewModel[]>("EnabledDayOfWeeks")]
public sealed partial class AlarmTimerEditDialog : ContentDialog
{
    public AlarmTimerEditDialog()
    {
        this.InitializeComponent();

        Loaded += AlarmTimerEditDialog_Loaded;
        Closing += AlarmTimerEditDialog_Closing;

        _soundContentPlayerService = Ioc.Default.GetRequiredService<ISoundContentPlayerService>();
        _soundSelectionItems = _soundContentPlayerService
            .GetAllSoundContents()
            .Select(x => new SoundSelectionItemViewModel { SoundSourceType = x.SoundSourceType, Label = x.Label, SoundContent = x.SoundParameter })
            .ToArray();
    }

    private void AlarmTimerEditDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        TryCencelTestPlaySound();
    }

    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly SoundSelectionItemViewModel[] _soundSelectionItems;

    private readonly TimeSpan[] _snoozeTimes = new TimeSpan[]
        {
            TimeSpan.Zero,
        }
        .Concat(AlarmTimerConstants.SnoozeTimes).ToArray();

    private SoundSelectionItemViewModel? _firstSelectedsoundSelectionItem;
    private TimeSpan? _snoozeTimeFirstSelected;


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

    private void AlarmTimerEditDialog_Loaded(object sender, RoutedEventArgs e)
    {
        ComboBox_SnoozeTime.SelectedItem = _snoozeTimeFirstSelected;
        ComboBox_SoundSelectionItem.SelectedItem = _firstSelectedsoundSelectionItem;
        UpdateTextBoxTtsVisibility();
    }


    public async Task<AlarmTimerDialogResult> ShowAsync(string dialogTitle, string timerTitle, TimeOnly dayOfTime, TimeSpan? snooze, IEnumerable<DayOfWeek> enabledDayOfWeeks, DayOfWeek firstDayOfWeek, SoundSourceType soundSourceType, string soundContent)
    {
        Title = dialogTitle;
        TextBox_EditTitle.Text = timerTitle;
        TimeSelectBox_TimeOfDay.Time = dayOfTime.ToTimeSpan();
        SetSoundSource(soundSourceType, soundContent);
        _snoozeTimeFirstSelected = _snoozeTimes.FirstOrDefault(x => x == snooze, _snoozeTimes.First());
        
        var enabledDayOfWeeksHashSet = enabledDayOfWeeks.ToHashSet();
        EnabledDayOfWeeks = firstDayOfWeek.ToWeek()
            .Select(x => new EnabledDayOfWeekViewModel(x) { IsEnabled = enabledDayOfWeeksHashSet.Contains(x) })
            .ToArray();

        TimeSelectBox_TimeOfDay.TimeChanged += TimeSelectBox_TimeOfDay_TimeChanged;
        ComboBox_SnoozeTime.SelectionChanged += ComboBox_SnoozeTime_SelectionChanged;
        
        var disposer = EnabledDayOfWeeks.Select(x => x.ObserveProperty(x => x.IsEnabled)).CombineLatestValuesAreAllFalse()
            .Where(_ => !_isRepeatChanging)
            .Subscribe(x =>
            {
                IsRepeat = !x;
            });

        try
        {
            if (await base.ShowAsync() is ContentDialogResult.Primary)
            {
                var (resultSoundSourceType, resultSoundContent) = GetSoundParameter();
                
                return new AlarmTimerDialogResult
                {
                    IsConfirmed = true,
                    Title = TextBox_EditTitle.Text,
                    TimeOfDay = TimeOnly.FromTimeSpan(TimeSelectBox_TimeOfDay.Time),
                    Snooze = ComboBox_SnoozeTime.SelectedItem as TimeSpan?,
                    EnabledDayOfWeeks = EnabledDayOfWeeks.Where(x => x.IsEnabled).Select(x => x.DayOfWeek).ToArray(),                    
                    SoundSourceType = resultSoundSourceType,
                    SoundContent = resultSoundContent,
                };
            }
            else
            {
                return new AlarmTimerDialogResult { IsConfirmed = false };
            }
        }
        finally
        {
            disposer.Dispose();
            TimeSelectBox_TimeOfDay.TimeChanged -= TimeSelectBox_TimeOfDay_TimeChanged;
            ComboBox_SnoozeTime.SelectionChanged -= ComboBox_SnoozeTime_SelectionChanged;
        }
    }



    private void ComboBox_SnoozeTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateIsPrimaryButtonEnabled();
    }

    bool _isRepeatChanging = false;

    private void CheckBox_IsRepeat_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var newValue = IsRepeat;
        _isRepeatChanging = true;
        foreach (var item in EnabledDayOfWeeks!)
        {
            item.IsEnabled = newValue;
        }

        _isRepeatChanging = false;
    }

    private void TimeSelectBox_TimeOfDay_TimeChanged(Controls.TimeSelectBox sender, Controls.TimeSelectBoxTimeValueChangedEventArgs args)
    {
        UpdateIsPrimaryButtonEnabled();
    }


    private void UpdateIsPrimaryButtonEnabled()
    {
        var enabled = IValidSoundSourceAndSoundContent();
        IsPrimaryButtonEnabled = enabled;
        Button_TestPlaySound.IsEnabled = enabled;
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


    private bool IValidSoundSourceAndSoundContent()
    {
        var selectedSoundItem = ComboBox_SoundSelectionItem.SelectedItem as SoundSelectionItemViewModel;
        if (selectedSoundItem == null)
        {
            return false;
        }
        if (selectedSoundItem.SoundSourceType == SoundSourceType.System)
        {
            return true;
        }
        else if (selectedSoundItem.SoundSourceType == SoundSourceType.Tts)
        {
            return IsValidInput(TextBox_Tts.Text);
        }
        else if (selectedSoundItem.SoundSourceType == SoundSourceType.TtsWithSSML)
        {
            return IsValidInput(TextBox_Tts.Text);
        }
        else if (selectedSoundItem.SoundSourceType == SoundSourceType.AudioFile)
        {
            return true;
        }
        else { throw new NotSupportedException(selectedSoundItem.SoundSourceType.ToString()); }
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

        (SoundSourceType soundSourceType, string soundContent) = GetSoundParameter();

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

    public static bool ConvertIsEnabled(bool? isChecked)
    {
        return isChecked ?? false;
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
