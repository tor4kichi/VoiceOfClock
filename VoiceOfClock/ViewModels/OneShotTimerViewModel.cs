using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;

public sealed partial class OneShotTimerViewModel : ObservableObject, IDisposable
{
    public OneShotTimerViewModel(OneShotTimerRunningInfo runningInfo, IMessenger messenger, Action<OneShotTimerViewModel> onDeleteAction)
    {
        RunningInfo = runningInfo;
        _messenger = messenger;
        _onDeleteAction = onDeleteAction;
        _time = RunningInfo.Time;
        _title = RunningInfo.Title;
        _remainingTime = RunningInfo.RemainingTime;
        _isTimerActive = RunningInfo.Time != RunningInfo.RemainingTime;

        RunningInfo.ObserveProperty(x => x.RemainingTime)
            .Subscribe(x => RemainingTime = x)
            .AddTo(_disposables);

        if (RunningInfo.IsRunning)
        {
            EndTime = TimeOnly.FromDateTime(RunningInfo.StartTime + Time);
        }

        RunningInfo.OnTimesUp += RunningInfo_OnTimesUp;
    }

    private void RunningInfo_OnTimesUp(object? sender, OneShotTimerRunningInfo e)
    {
#if DEBUG
        _sw.Stop();
        Debug.WriteLine($"Time: {Time:T} / Real: {_sw.Elapsed:T}");
#endif
        // TODO: 画面上の通知動作を実行                

    }

    private readonly CompositeDisposable _disposables = new();
    private readonly IMessenger _messenger;
    private readonly Action<OneShotTimerViewModel> _onDeleteAction;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RewindTimerCommand))]
    private TimeSpan _time;

    partial void OnTimeChanged(TimeSpan value)
    {
        RunningInfo.Time = value;
        RemainingTime = value;
    }


    [ObservableProperty]
    private string _title;


    partial void OnTitleChanged(string value)
    {
        RunningInfo.Title = value;
    }

    [ObservableProperty]   
    private TimeSpan _remainingTime;

    [ObservableProperty]
    private TimeOnly? _endTime;

    [ObservableProperty]
    private bool _isEditting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RewindTimerCommand))]
    private bool _isTimerActive;


    partial void OnRemainingTimeChanged(TimeSpan value)
    {
        IsTimerActive = value != Time;
    }

    public OneShotTimerRunningInfo RunningInfo { get; }

    public static string ConvertTime(TimeSpan time)
    {
        return time.TrimMilliSeconds().ToString("c");
    }

    public static string ConvertTime(TimeOnly? time)
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
    void ToggleTimerStartAndStop()
    {
        if (RunningInfo.IsRunning)
        {
#if DEBUG
            _sw.Stop();
#endif

            RunningInfo.StopTimer();
        }
        else
        {
#if DEBUG
            _sw.Start();
#endif
            RunningInfo.StartTimer();

            RemainingTime = RunningInfo.RemainingTime;
            EndTime = TimeOnly.FromDateTime(RunningInfo.StartTime + Time);
        }
    }   


    [RelayCommand(CanExecute = nameof(CanExecuteRewindTimer))]
    void RewindTimer()
    {
        RunningInfo.RewindTimer();
        RemainingTime = Time;
        EndTime = TimeOnly.FromDateTime(RunningInfo.StartTime + Time);
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
        RunningInfo.OnTimesUp -= RunningInfo_OnTimesUp;
    }
}
