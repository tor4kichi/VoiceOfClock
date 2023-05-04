using CommunityToolkit.Mvvm.DependencyInjection;
using DependencyPropertyGenerator;
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
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Pickers;

namespace VoiceOfClock.Views;

[DependencyProperty<TimeSpan>("Duration")]
[DependencyProperty<string>("FilePath")]
[DependencyProperty<TimeSpan>("AudioBegin")]
[DependencyProperty<TimeSpan>("AudioEnd")]
[DependencyProperty<string>("Label")]
[DependencyProperty<double>("SoundVolume")]
[DependencyProperty<bool>("NowLoading")]
[DependencyProperty<TimeSpan>("CurrentPlaybackPosition")]
public sealed partial class AudioSoundSourceEditDialog : ContentDialog
{
    public AudioSoundSourceEditDialog()
    {
        this.InitializeComponent();

        Opened += AudioSoundSourceEditDialog_Opened;
        Closing += AudioSoundSourceEditDialog_Closing;
    }

    private bool _nowPositionChanging = false;
    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _nowPositionChanging = true;
            CurrentPlaybackPosition = sender.Position;
            _nowPositionChanging = false;
        });        
    }

    private void _mediaPlayer_BufferingEnded(MediaPlayer sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            NowLoading = false;
            RefreshAudioPlayPauseToggle();
        });
    }

    private void _mediaPlayer_BufferingStarted(MediaPlayer sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            NowLoading = true;
            RefreshAudioPlayPauseToggle();
        });
    }


    private CancellationTokenSource? _cts;

    private async void AudioSoundSourceEditDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;
        _mediaPlayer = Ioc.Default.GetRequiredService<MediaPlayer>();
        _mediaPlayer.AutoPlay = false;
        _mediaPlayer.Volume = SoundVolume;

        _mediaPlayer.BufferingStarted += _mediaPlayer_BufferingStarted;
        _mediaPlayer.BufferingEnded += _mediaPlayer_BufferingEnded;
        _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        CurrentPlaybackPosition = AudioBegin;
        if (File.Exists(FilePath))
        {
            var source = MediaSource.CreateFromUri(new Uri(FilePath));
            _mediaPlayer.Source = source;
            try
            {
                await source.OpenAsync().AsTask(cancellationToken);
                _mediaPlayer.PlaybackSession.Position = CurrentPlaybackPosition;
                Duration = source.Duration ?? TimeSpan.Zero;
                SetButtonsEnabling(true);
            }
            catch (OperationCanceledException)
            {

            }
        }
        else
        {
            SetButtonsEnabling(false);

            await ChoiceFileAsync(cancellationToken);
        }
    }

    private void AudioSoundSourceEditDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        if (_mediaPlayer?.Source is MediaPlaybackItem item)
        {
            (item.Source as IDisposable)?.Dispose();
        }
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;
    }

    partial void OnAudioBeginChanged()
    {
        if (!IsLoaded) { return; }

        SetButtonsEnabling(true);
    }

    partial void OnAudioEndChanged()
    {
        if (!IsLoaded) { return; }

        SetButtonsEnabling(true);
    }

    partial void OnCurrentPlaybackPositionChanged(TimeSpan newValue)
    {
        if (_mediaPlayer == null) { return; }
        if (_nowPositionChanging) { return; }

        _mediaPlayer.PlaybackSession.Position = newValue;
    }

    partial void OnSoundVolumeChanged(double newValue)
    {
        if (_mediaPlayer == null) { return; }

        _mediaPlayer.Volume = newValue;
    }

    partial void OnFilePathChanged(string? newValue)
    {
        RefreshAudioPlayPauseToggle();
    }

    void RefreshAudioPlayPauseToggle()
    {
        Button_AudioPlayPauseToggle.IsEnabled = File.Exists(FilePath) && !NowLoading;
    }

    public static readonly string[] SupprotedAudioFileTypes = new[] 
    {
        ".asf", ".wma", ".wmv", ".wm",
        ".mpg", ".mpeg", ".m1v", ".mp2", ".mp3", ".mpa", ".mpe", ".m3u",
        ".wav",
        ".mov",
        ".m4a",
        ".mp4", ".m4v", ".mp4v", ".3g2", ".3gp2", ".3gp", ".3gpp",
        ".aac", ".adt", ".adts",
        ".flac",
    };
    private MediaPlayer? _mediaPlayer;

    private async void Button_ChangeFilePath_Tapped(object sender, TappedRoutedEventArgs e)
    {
        await ChoiceFileAsync(_cts?.Token ?? default);
    }

    private async Task ChoiceFileAsync(CancellationToken ct)
    {
        if (_mediaPlayer == null) { return; }

        var picker = new FileOpenPicker();
        App.Current.InitializeWithWindow(picker);

        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

        try
        {
            // supported filetypes 
            // https://support.microsoft.com/en-us/topic/file-types-supported-by-windows-media-player-32d9998e-dc8f-af54-7ba1-e996f74375d9        
            foreach (var fileType in SupprotedAudioFileTypes)
            {
                picker.FileTypeFilter.Add(fileType);
            }

            var result = await picker.PickSingleFileAsync();
            if (result == null) { return; }

            FilePath = result?.Path ?? string.Empty;

            var mediaSource = MediaSource.CreateFromStorageFile(result);
            _mediaPlayer.Source = mediaSource;
            try
            {
                await mediaSource.OpenAsync().AsTask(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            
            Duration = mediaSource.Duration ?? throw new Exception();
            AudioBegin = TimeSpan.Zero;
            AudioEnd = Duration;
            Label = Path.GetFileNameWithoutExtension(FilePath);
            SetButtonsEnabling(true);
        }
        catch
        {
            SetButtonsEnabling(false);
            Duration = TimeSpan.Zero;
            FilePath = string.Empty;
            AudioBegin = TimeSpan.Zero;
            AudioEnd = TimeSpan.Zero;
        }
    }


    void SetButtonsEnabling(bool enable)
    {
        Button_AudioPlayPauseToggle.IsEnabled = enable;
        Slider_PlaybackPosition.IsEnabled = enable;
        RangeSelector_AudioSpan.IsEnabled = enable;
        Button_SetBeginPosition.IsEnabled = enable;
        Button_SetEndPosition.IsEnabled = enable;
        IsPrimaryButtonEnabled = enable && AudioBegin != AudioEnd && Label.Length > 0;
    }

    private void Button_AudioPlayPauseToggle_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_mediaPlayer == null) { return; }
        if (_mediaPlayer.Source == null) { return; }

        if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        {            
            _mediaPlayer.Pause();
        }
        else if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
        {            
            _mediaPlayer.Play();
        }
    }

    private void Button_SetBeginPosition_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var prevBeginPos = AudioBegin;
        AudioBegin = CurrentPlaybackPosition;
        if (AudioBegin > AudioEnd)
        {
            AudioEnd = prevBeginPos;
        }
    }

    private void Button_SetEndPosition_Tapped(object sender, TappedRoutedEventArgs e)
    {
        AudioEnd = CurrentPlaybackPosition;
        if (AudioBegin > AudioEnd)
        {
            AudioBegin = TimeSpan.Zero;
        }
    }

    private void TextBox_Title_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetButtonsEnabling(true);
    }
}
