using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using I18NPortable;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;


public sealed partial class AudioSoundSourcePageViewModel : ObservableRecipient
{
    private readonly AudioSoundSourceRepository _audioSoundSourceRepository;
    private readonly IAudioSoundSourceDialogService _audioSoundSourceDialogService;

    public ReadOnlyObservableCollection<AudioSoundSourceViewModel> Items { get; }

    private readonly ObservableCollection<AudioSoundSourceViewModel> _items;

    public AudioSoundSourcePageViewModel(
        AudioSoundSourceRepository audioSoundSourceRepository
        , IAudioSoundSourceDialogService audioSoundSourceDialogService
        )
    {
        _audioSoundSourceRepository = audioSoundSourceRepository;
        _audioSoundSourceDialogService = audioSoundSourceDialogService;
        _items = new ObservableCollection<AudioSoundSourceViewModel>();
        Items = new ReadOnlyObservableCollection<AudioSoundSourceViewModel>(_items);
    }

    protected override void OnActivated()
    {
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
        var result = await _audioSoundSourceDialogService.ShowAsync("AudioSoundSource_DialogTitle_New".Translate(), string.Empty, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, string.Empty, 1.0);
        if (result.IsConfirmed)
        {
            var newEntity = _audioSoundSourceRepository.CreateItem(
                new AudioSoundSourceEntity(
                    result.FilePath
                    , result.Duration
                    )
                {
                    Title = result.Title,
                    AudioSpan = result.AudioSpan,
                    SoundVolume = result.SoundVolume,
                });

            var itemVM = new AudioSoundSourceViewModel(newEntity, _audioSoundSourceRepository);
            _items.Add(itemVM);
        }
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
    public AudioSoundSourceViewModel(AudioSoundSourceEntity entity, AudioSoundSourceRepository repository)
    {
        Entity = entity;
        _repository = repository;
        _filePath = Entity.FilePath;
        _duration = Entity.Duration;
        _audioSpanBegin = Entity.AudioSpan.Begin;
        _audioSpanEnd = Entity.AudioSpan.End;
        _title = Entity.Title;
        _soundVolume= Entity.SoundVolume;
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
}
