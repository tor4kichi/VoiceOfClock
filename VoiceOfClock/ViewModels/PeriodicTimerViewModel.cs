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

public partial class PeriodicTimerViewModel : ObservableObject,
    IRecipient<RunningPeriodicTimerUpdated>

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
        
        var res = _messenger.Send<RequestRunningPeriodicTimer>(new RequestRunningPeriodicTimer(_entity.Id));
        if (res.HasReceivedResponse)
        {
            _nextTime = res.Response.NextTime;
            _isInsidePeriod = res.Response.IsInsidePeriod;
        }
    }

    [ObservableProperty]
    private bool _isEditting;

    [ObservableProperty]
    private DateTime _nextTime;

    [ObservableProperty]
    private bool _isInsidePeriod;





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
        _entity.IntervalTime = _intervalTime;
        _entity.StartTime = _startTime;
        _entity.EndTime = _endTime;
        _entity.Title = _title;
        
        _repository.UpdateItem(_entity);
        _messenger.Send(new PeriodicTimerUpdated(_entity));
    }

    [RelayCommand]
    void Delete()
    {
        _repository.DeleteItem(_entity.Id);
        _messenger.Send(new PeriodicTimerRemoved(_entity));
    }

    void IRecipient<RunningPeriodicTimerUpdated>.Receive(RunningPeriodicTimerUpdated message)
    {
        NextTime = message.Value.NextTime;
        IsInsidePeriod = message.Value.IsInsidePeriod;
    }
}
