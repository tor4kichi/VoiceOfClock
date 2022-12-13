using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Reactive.Bindings.Extensions;
using System;
using System.Threading;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VoiceOfClock.Core.Domain;
using VoiceOfClock.UseCases;
using System.Threading.Tasks;

namespace VoiceOfClock.ViewModels;

[ObservableObject]
public sealed partial class OneShotTimerViewModel : IDisposable
{
    public OneShotTimerEntity Entity { get; }

    private readonly CompositeDisposable _disposables = new();
    private readonly OneShotTimerLifetimeManager _oneShotTimerLifetimeManager;
    private readonly IMessenger _messenger;
    private readonly Action<OneShotTimerViewModel> _onDeleteAction;

    public OneShotTimerViewModel(OneShotTimerEntity entity, OneShotTimerLifetimeManager oneShotTimerLifetimeManager, IMessenger messenger, Action<OneShotTimerViewModel> onDeleteAction)
    {
        Entity = entity;
        _oneShotTimerLifetimeManager = oneShotTimerLifetimeManager;
        _messenger = messenger;
        _onDeleteAction = onDeleteAction;
        _time = Entity.Time;
        _title = Entity.Title;        
        (_isTimerActive, _endTime, _remainingTime) = oneShotTimerLifetimeManager.GetTimerRunningInfo(Entity);
        _isRunning = false;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RewindTimerCommand))]
    private TimeSpan _time;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]   
    private TimeSpan _remainingTime;

    [ObservableProperty]
    private DateTime? _endTime;

    [ObservableProperty]
    private bool _isEditting;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RewindTimerCommand))]
    private bool _isTimerActive;


    public void RefrectValues()
    {
        Title = Entity.Title;
        Time = Entity.Time;
        RemainingTime = Entity.Time;
    }

    public void UpdateRemainingTime()
    {
        if (IsRunning is false) { return; }

        if (EndTime.HasValue)
        {
            var remainingTime = (EndTime.Value - DateTime.Now);
            if (remainingTime > TimeSpan.Zero)
            {
                RemainingTime = remainingTime;
            }
            else
            {
                RemainingTime = TimeSpan.Zero;
                EndTime = null;
                IsRunning = false;
            }
        }
        else
        {
            IsRunning = false;
        }
    }

    partial void OnRemainingTimeChanged(TimeSpan value)
    {
        IsTimerActive = value != Time;
    }

    public static string ConvertTime(TimeSpan time)
    {
        return time.TrimMilliSeconds().ToString("c");
    }

    public static string ConvertTime(TimeOnly? time)
    {
        return time?.ToString("T") ?? string.Empty;
    }

    public static string ConvertTime(DateTime? time)
    {
        return time?.ToString("T") ?? string.Empty;
    }

    public static double ConvertToDouble(TimeSpan time)
    {
        return time.TotalSeconds;
    }

    public static double ConvertToDouble(TimeOnly? time)
    {
        return time?.ToTimeSpan().TotalSeconds ?? 0.0d;
    }

#if DEBUG
    private readonly Stopwatch _sw = new ();
#endif
    [RelayCommand]
    async Task ToggleTimerStartAndStop()
    {
        if (IsRunning)
        {
            IsRunning = false;
            await _oneShotTimerLifetimeManager.PauseTimer(Entity);            
#if DEBUG
            _sw.Stop();
#endif            
        }
        else
        {
#if DEBUG
            _sw.Start();
#endif
            await _oneShotTimerLifetimeManager.StartTimer(Entity, RemainingTime);
            (IsTimerActive, EndTime, RemainingTime) = _oneShotTimerLifetimeManager.GetTimerRunningInfo(Entity);

            IsRunning = true;
        }
    }   


    [RelayCommand(CanExecute = nameof(CanExecuteRewindTimer))]
    async Task RewindTimer()
    {
        await _oneShotTimerLifetimeManager.RewindTimer(Entity, IsRunning);
        (IsTimerActive, EndTime, RemainingTime) = _oneShotTimerLifetimeManager.GetTimerRunningInfo(Entity);
#if DEBUG
        _sw.Reset();
#endif
    }

    bool CanExecuteRewindTimer()
    {
        return IsTimerActive;
    }

    //[RelayCommand]
    //void ShowWithCompactOverlay()
    //{

    //}

    //[RelayCommand]
    //void ShowWithFillWindow()
    //{

    //}

    //[RelayCommand]
    //void ShowWithDefault()
    //{

    //}

    [RelayCommand]
    void Delete()
    {
        _onDeleteAction(this);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    
}
