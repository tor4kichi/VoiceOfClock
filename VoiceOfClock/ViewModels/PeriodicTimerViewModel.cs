using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;

public partial class PeriodicTimerViewModel : ObservableObject
{
    private readonly PeriodicTimerEntity _entity;
    private readonly PeriodicTimerRepository _repository;
    private readonly IMessenger _messenger;

    public PeriodicTimerViewModel(PeriodicTimerEntity entity, PeriodicTimerRepository repository, IMessenger messenger)
    {
        _entity = entity;
        _repository = repository;
        _messenger = messenger;
        _isEnabled = _entity.IsEnabled;
        _intervalTime = _entity.IntervalTime;
        _startTime = _entity.StartTime;
        _endTime = _entity.EndTime;
        _title = _entity.Title;
    }

    [ObservableProperty]
    private bool _isEditting;

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        _entity.IsEnabled = value;
        _repository.UpdateItem(_entity);
        _messenger.Send(new PeriodicTimerUpdated(_entity));
    }


    [ObservableProperty]
    private TimeSpan _intervalTime;

    [ObservableProperty]
    private TimeSpan _startTime;

    [ObservableProperty]
    private TimeSpan _endTime;


    [ObservableProperty]
    private string _title;

    public void UpdateEntity()
    {
        _repository.UpdateItem(_entity);
        _messenger.Send(new PeriodicTimerUpdated(_entity));
    }

    [RelayCommand]
    void Delete()
    {
        _repository.DeleteItem(_entity.Id);
        _messenger.Send(new PeriodicTimerRemoved(_entity));
    }
}
