using CommunityToolkit.Mvvm.DependencyInjection;
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
using System.Threading;
using VoiceOfClock.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Pickers;
using DependencyPropertyGenerator;
using Microsoft.UI.Dispatching;
using Reactive.Bindings.Extensions;
using VoiceOfClock.Contracts.Services;
using I18NPortable;

namespace VoiceOfClock.Views;

[DependencyProperty<TimeSpan>("Duration")]
[DependencyProperty<string>("FilePath")]
[DependencyProperty<string>("Label")]
[DependencyProperty<double>("SoundVolume")]
[DependencyProperty<bool>("NowLoading")]
[DependencyProperty<TimeSpan>("AudioBegin")]
[DependencyProperty<TimeSpan>("AudioEnd")]
[DependencyProperty<TimeSpan>("CurrentPlaybackPosition")]
public sealed partial class AudioSoundSourcePage : Page
{
    public AudioSoundSourcePage()
    {
        this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<AudioSoundSourcePageViewModel>();
        _mediaPlayer = new MediaPlayer() 
        {
            
        };

        _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        _dispatcherQueue = DispatcherQueue;

        
        _vm.ObserveProperty(x => x.SelectedAudioSoundSourceVM)
            .Subscribe(x => 
            {            
                if (x != null)
                {
                    Duration = x.Duration;
                    FilePath = x.FilePath;
                    Label = x.Title;
                    SoundVolume = x.SoundVolume;
                    AudioBegin = x.AudioSpanBegin;
                    AudioEnd = x.AudioSpanEnd;
                    
                    CurrentPlaybackPosition = TimeSpan.Zero;
                    _mediaPlayer.Volume = SoundVolume;

                    LoadMedia(FilePath);
                }
            });
    }

    private void LoadMedia(string filePath)
    {
        if (_mediaPlayer.Source is IDisposable disposable)
        {
            disposable.Dispose();
        }
        if (Uri.TryCreate(filePath, uriKind: UriKind.Absolute, out Uri uri))
        {
            _mediaPlayer.Source = MediaSource.CreateFromUri(uri);            
        }   
        else
        {
            _mediaPlayer.Source = null;
        }
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() => 
        {
            _nowPositionChanging = true;
            try
            {
                CurrentPlaybackPosition = sender.Position;
            }
            finally
            {
                _nowPositionChanging = false;
            }
        });
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

        var source = _mediaPlayer.Source;
        _mediaPlayer.Source = null;
        if (source is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else if (source is MediaPlaybackItem item
            && item.Source is IDisposable disposable1
            )
        {
            disposable1.Dispose();
        }        
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


    #region Edit Audio
    
    private bool _nowPositionChanging = false;
    private CancellationTokenSource? _cts;
    private MediaPlayer? _mediaPlayer;

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
        if (_vm.SelectedAudioSoundSourceVM is { } itemVM)
        {
            itemVM.SoundVolume = newValue;
        }
    }

    partial void OnFilePathChanged(string? newValue)
    {
        RefreshAudioPlayPauseToggle();
    }

    void RefreshAudioPlayPauseToggle()
    {
        Button_AudioPlayPauseToggle.IsEnabled = File.Exists(FilePath) && !NowLoading;
    }

    private readonly DispatcherQueue _dispatcherQueue;

    private async void Button_ChangeFilePath_Tapped(object sender, TappedRoutedEventArgs e)
    {
        await ChoiceFileAsync(_cts?.Token ?? default);
    }

    private async Task ChoiceFileAsync(CancellationToken ct)
    {

        var dialogService = Ioc.Default.GetRequiredService<IAudioSoundSourceDialogService>();
        try
        {            
            if (await dialogService.ChoiceFileAsync("AudioSoundSource_DialogTitle_Edit".Translate()) is not { } file) 
            {
                return; 
            }

            var mediaSource = MediaSource.CreateFromStorageFile(file);            
            _mediaPlayer.Source = mediaSource;

            FilePath = file?.Path ?? string.Empty;

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
//        IsPrimaryButtonEnabled = enable && AudioBegin != AudioEnd && Label.Length > 0;
    }
    
    private void Button_AudioPlayPauseToggle_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_mediaPlayer == null) { return; }
        if (TryOpenSource(FilePath, TimeSpan.Zero, Duration))
        {
            _mediaPlayer.Play();
        }
        else if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        {
            _mediaPlayer.Pause();
        }
        else if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
        {
            _mediaPlayer.Play();
        }
    }

    TimeSpan _rangedPlayAudioBegin;
    TimeSpan _rangedPlayAudioEnd;
    string? _playingFilePath;
    private bool TryOpenSource(string filePath, TimeSpan start, TimeSpan end)
    {
        if (_playingFilePath != filePath
            || start != _rangedPlayAudioBegin
            || end != _rangedPlayAudioEnd
            )
        {
            if (_mediaPlayer.Source is IDisposable disposable)
            {
                _mediaPlayer.Source = null;
                disposable.Dispose();
            }
            else if (_mediaPlayer.Source is MediaPlaybackItem item
                && item.Source is IDisposable disposable1
                )
            {
                _mediaPlayer.Source = null;
                disposable1.Dispose();
            }
            var mediaSource = MediaSource.CreateFromUri(new Uri(filePath));
            _mediaPlayer.Source = new MediaPlaybackItem(mediaSource, start, end);
            _rangedPlayAudioBegin = start;
            _rangedPlayAudioEnd = end;
            _playingFilePath = filePath;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Button_AudioRangedPlayPauseToggle_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_mediaPlayer == null) { return; }
        if (TryOpenSource(FilePath, AudioBegin, AudioEnd))
        {
            _mediaPlayer.Play();
        }
        else if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
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
        var prevBeginPos = _vm.SelectedAudioSoundSourceVM.AudioSpanBegin;
        _vm.SelectedAudioSoundSourceVM.AudioSpanBegin = _mediaPlayer.PlaybackSession.Position;
        if (_vm.SelectedAudioSoundSourceVM.AudioSpanBegin > _vm.SelectedAudioSoundSourceVM.AudioSpanEnd)
        {
            _vm.SelectedAudioSoundSourceVM.AudioSpanEnd = prevBeginPos;
        }
    }

    private void Button_SetEndPosition_Tapped(object sender, TappedRoutedEventArgs e)
    {
        _vm.SelectedAudioSoundSourceVM.AudioSpanEnd = _mediaPlayer.PlaybackSession.Position;
        if (_vm.SelectedAudioSoundSourceVM.AudioSpanBegin > _vm.SelectedAudioSoundSourceVM.AudioSpanEnd)
        {
            _vm.SelectedAudioSoundSourceVM.AudioSpanBegin = TimeSpan.Zero;
        }
    }

    private void TextBox_Title_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetButtonsEnabling(true);
    }

    #endregion

    private void Button_AudioMoveNext_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_mediaPlayer?.PlaybackSession.CanSeek ?? true)
        {
            _mediaPlayer.Position += TimeSpan.FromSeconds(10);
        }
    }

    private void Button_AudioMovePreview_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (_mediaPlayer?.PlaybackSession.CanSeek ?? true)
        {
            _mediaPlayer.Position -= TimeSpan.FromSeconds(10);
        }
    }

    private void Button_Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedAudioSoundSourceVM is { } itemVM)
        {
            using (itemVM.DeferUpdate())
            {
                itemVM.Title = Label;
                itemVM.Duration = Duration;
                itemVM.AudioSpanBegin = AudioBegin;
                itemVM.AudioSpanEnd = AudioEnd;
                itemVM.FilePath = FilePath;
                itemVM.SoundVolume = SoundVolume;
            }
        }
    }

    private void Button_Cancel_Click(object sender, RoutedEventArgs e)
    {
        _vm.SelectedAudioSoundSourceVM = null;
    }

    private void Button_PlayWithAudioSpan_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var itemVM = (sender as FrameworkElement)!.DataContext as AudioSoundSourceViewModel;
        if (TryOpenSource(itemVM.FilePath, itemVM.AudioSpanBegin, itemVM.AudioSpanEnd))
        {
            _mediaPlayer.Play();
        }
        else if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        {
            _mediaPlayer.Pause();
        }
        else if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
        {
            _mediaPlayer.Play();
        }
    }

    private void Button_Edit_Tapped(object sender, TappedRoutedEventArgs e)
    {
        _vm.SelectedAudioSoundSourceVM = (sender as FrameworkElement)!.DataContext as AudioSoundSourceViewModel;
    }

    private void AdaptiveGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem == null)
        {
            _vm.AddNewAudioSoundSourceCommand.Execute(null);
        }
    }
}
