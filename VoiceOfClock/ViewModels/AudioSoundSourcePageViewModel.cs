using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using I18NPortable;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;

namespace VoiceOfClock.ViewModels;


public sealed partial class AudioSoundSourcePageViewModel : ObservableRecipient
{
    private readonly AudioSoundSourceRepository _audioSoundSourceRepository;
    private readonly IAudioSoundSourceDialogService _audioSoundSourceDialogService;

    public ReadOnlyObservableCollection<object> Items { get; }

    private readonly ObservableCollection<object> _items;

    [ObservableProperty]
    private AudioSoundSourceViewModel? _selectedAudioSoundSourceVM;

    public AudioSoundSourcePageViewModel(
        AudioSoundSourceRepository audioSoundSourceRepository
        , IAudioSoundSourceDialogService audioSoundSourceDialogService
        )
    {
        _audioSoundSourceRepository = audioSoundSourceRepository;
        _audioSoundSourceDialogService = audioSoundSourceDialogService;
        _items = new ObservableCollection<object>();
        Items = new ReadOnlyObservableCollection<object>(_items);
    }

    protected override void OnActivated()
    {
        _items.Add(null);
        List<AudioSoundSourceEntity> entities = _audioSoundSourceRepository.ReadAllItems();
        foreach (var item in entities)
        {
            var itemVM = new AudioSoundSourceViewModel(item, _audioSoundSourceRepository);
            _items.Add(itemVM);
        }
        
        base.OnActivated();
    }

    protected override void OnDeactivated()
    {
        foreach (var item in Items)
        {
            //(item as IDisposable)?.Dispose();
        }
        _items.Clear();
        base.OnDeactivated();
    }


    [RelayCommand]
    async Task AddNewAudioSoundSource()
    {
        if (await _audioSoundSourceDialogService.ChoiceFileAsync("AudioSoundSource_DialogTitle_New".Translate()) is not { } file)
        {
            return;
        }

        var musicProps = await file.Properties.GetMusicPropertiesAsync();
        var newEntity = _audioSoundSourceRepository.CreateItem(
            new AudioSoundSourceEntity(
                file.Path
                , musicProps.Duration
                )
            {
                Title = musicProps.Title,
                AudioSpan = new AudioSpan(TimeSpan.Zero, musicProps.Duration),
                SoundVolume = 1.0,
            });

        var itemVM = new AudioSoundSourceViewModel(newEntity, _audioSoundSourceRepository);
        _items.Add(itemVM);
    }

    [RelayCommand]
    async Task EditAudioSoundSource(AudioSoundSourceViewModel itemVM)
    {
        var result = await _audioSoundSourceDialogService.ShowAsync("AudioSoundSource_DialogTitle_Edit".Translate(), itemVM.FilePath, itemVM.Duration, itemVM.AudioSpanBegin, itemVM.AudioSpanEnd, itemVM.Title ?? string.Empty, itemVM.SoundVolume);
        if (result.IsConfirmed)
        {
            using (itemVM.DeferUpdate())
            {
                itemVM.Title = result.Title;
                itemVM.Duration = result.Duration;
                itemVM.FilePath = result.FilePath;
                itemVM.AudioSpanBegin = result.AudioSpan.Begin;
                itemVM.AudioSpanEnd = result.AudioSpan.End;        
                itemVM.SoundVolume = result.SoundVolume;
            }
        }
    }

    [RelayCommand]
    void DeleteAudioSoundSource(AudioSoundSourceViewModel itemVM)
    {
        // TODO: オーディオソースを利用しているタイマーを列挙する
        _audioSoundSourceRepository.DeleteItem(itemVM.Entity.Id);
        _items.Remove(itemVM);
    }
}


[ObservableObject]
public sealed partial class AudioSoundSourceViewModel  : DeferUpdatable
{
    public AudioSoundSourceViewModel(
        AudioSoundSourceEntity entity, 
        AudioSoundSourceRepository repository
        )
    {
        Entity = entity;
        _repository = repository;
        _filePath = Entity.FilePath;
        _duration = Entity.Duration;
        _audioSpanBegin = Entity.AudioSpan.Begin;
        _audioSpanEnd = Entity.AudioSpan.End;
        _title = Entity.Title;
        _soundVolume= Entity.SoundVolume;
        RangedDuration = ConvertRangedDurationTime();
    }

    public AudioSoundSourceEntity Entity { get; }
    private readonly AudioSoundSourceRepository _repository;

    void Save()
    {
        _repository.UpdateItem(Entity);
    }

    protected override void OnDeferUpdate()
    {
        Save();
    }

    [ObservableProperty]
    private string _filePath;

    partial void OnFilePathChanged(string value)
    {
        Entity.FilePath = value;
        
        if (!NowDeferUpdateRequested)
        {
            Save();
        }
    }

    [ObservableProperty]
    private TimeSpan _duration;

    partial void OnDurationChanged(TimeSpan value)
    {
        Entity.Duration= value;
        RangedDuration = ConvertRangedDurationTime();

        if (!NowDeferUpdateRequested)
        {
            Save();
        }
    }

    [ObservableProperty]
    private TimeSpan _audioSpanBegin;

    partial void OnAudioSpanBeginChanged(TimeSpan value)
    {
        Entity.AudioSpan = Entity.AudioSpan with { Begin = value };
        RangedDuration = ConvertRangedDurationTime();

        if (!NowDeferUpdateRequested)
        {
            Save();
        }
    }


    [ObservableProperty]
    private TimeSpan _audioSpanEnd;

    partial void OnAudioSpanEndChanged(TimeSpan value)
    {
        Entity.AudioSpan = Entity.AudioSpan with { End = value };
        RangedDuration = ConvertRangedDurationTime();

        if (!NowDeferUpdateRequested)
        {
            Save();
        }
    }

    [ObservableProperty]
    private string? _title;

    partial void OnTitleChanged(string? value)
    {
        Entity.Title = value;

        if (!NowDeferUpdateRequested)
        {
            Save();
        }
    }


    [ObservableProperty]
    private double _soundVolume;

    partial void OnSoundVolumeChanged(double value)
    {
        Entity.SoundVolume = value;
        
        if (!NowDeferUpdateRequested)
        {
            Save();
        }
    }

    public string ConvertShortTime(TimeSpan time)
    {
        return time.TrimMilliSeconds().ToString("T");
    }


    [ObservableProperty]
    private TimeSpan _rangedDuration;

    public TimeSpan ConvertRangedDurationTime()
    {
        var beginTime = AudioSpanBegin;
        var endTime = AudioSpanEnd;
        if (beginTime ==  TimeSpan.Zero
            && endTime == Duration
            )
        {
            // 全体 Durationを返す
            return Duration;
        }
        else if (beginTime == endTime)
        {
            // BeginTimeからDurationまでの長さ
            return (Duration - beginTime);
        }
        else
        {
            var duration = endTime - beginTime;
            Guard.IsGreaterThan(duration, TimeSpan.Zero);
            return duration;
        }
    }
}

public abstract class DeferUpdatable
{
    public IDisposable DeferUpdate()
    {
        Guard.IsFalse(NowDeferUpdateRequested, nameof(NowDeferUpdateRequested));

        NowDeferUpdateRequested = true;
        return Disposable.Create(OnDeferUpdate_Internal);
    }

    bool _nowDeferUpdateRequested;
    protected bool NowDeferUpdateRequested
    {
        get => _nowDeferUpdateRequested;
        private set => _nowDeferUpdateRequested = value;
    }

    private void OnDeferUpdate_Internal()
    {
        NowDeferUpdateRequested = false;
        OnDeferUpdate();
    }
    protected abstract void OnDeferUpdate();
}