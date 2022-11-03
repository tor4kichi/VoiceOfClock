using CommunityToolkit.Mvvm.Messaging;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace VoiceOfClock.UseCases;


static class OneShotTimerConstants
{
    public const int UpdateFPS = 6;
}

public sealed class OneShotTimerLifetimeManager : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
{
    private readonly IMessenger _messenger;
    private readonly OneShotTimerRepository _oneShotTimerRepository;
    private readonly OneShotTimerRunningRepository _oneShotTimerRunningRepository;

    public OneShotTimerLifetimeManager(
        IMessenger messenger,
        OneShotTimerRepository oneShotTimerRepository,
        OneShotTimerRunningRepository oneShotTimerRunningRepository
        )
    {
        _messenger = messenger;
        _oneShotTimerRepository = oneShotTimerRepository;
        _oneShotTimerRunningRepository = oneShotTimerRunningRepository;

        _timers = new ObservableCollection<OneShotTimerRunningInfo>();
        Timers = new ReadOnlyObservableCollection<OneShotTimerRunningInfo>(_timers);
    }

    public ReadOnlyObservableCollection<OneShotTimerRunningInfo> Timers { get; }
    public ObservableCollection<OneShotTimerRunningInfo> _timers;

    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);

        var timers = _oneShotTimerRepository.ReadAllItems();
        foreach (var timer in timers)
        {
            _timers.Add(new OneShotTimerRunningInfo(timer, _oneShotTimerRepository, _oneShotTimerRunningRepository, _messenger));
        }
    }

    void IApplicationLifeCycleAware.Resuming()
    {
        
    }

    void IApplicationLifeCycleAware.Suspending()
    {
        
    }

    void IRecipient<ActiveTimerCollectionRequestMessage>.Receive(ActiveTimerCollectionRequestMessage message)
    {
        foreach (var timer in _timers)
        {
            if (timer.IsRunning)
            {
                message.Reply(timer);
            }
        }
    }

    public OneShotTimerRunningInfo CreateTimer(string title, TimeSpan time)
    {
        var entity = _oneShotTimerRepository.CreateItem(new OneShotTimerEntity() { Title = title, Time = time });
        var runningInfo = new OneShotTimerRunningInfo(entity, _oneShotTimerRepository, _oneShotTimerRunningRepository, _messenger);
        _timers.Add(runningInfo);
        return runningInfo;
    }


    public void DeleteTimer(OneShotTimerRunningInfo info)
    {
        _oneShotTimerRepository.DeleteItem(info.EntityId);
        _oneShotTimerRunningRepository.DeleteItem(info.EntityId);

        foreach (var remItem in _timers.Where(x => x.EntityId == info.EntityId).ToArray())
        {
            _timers.Remove(remItem);
        }
    }

}
