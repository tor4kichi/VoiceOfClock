using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;

public interface IOneShotTimerDialogService
{
    Task<OneShotTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle,TimeSpan time, SoundSourceType soundSourceType, string soundParameter);
}

public sealed class OneShotTimerDialogResult
{
    public bool IsConfirmed { get; init; }
    public string Title { get; init; }
    public TimeSpan Time { get; init; }
    public SoundSourceType SoundSourceType { get; init; }
    public string SoundParameter { get; init; }
}

public sealed partial class OneShotTimerPageViewModel : ObservableRecipient
{
    private readonly OneShotTimerLifetimeManager _oneShotTimerLifetimeManager;
    private readonly IOneShotTimerDialogService _oneShotTimerDialogService;

    [ObservableProperty]
    private ReadOnlyReactiveCollection<OneShotTimerViewModel> _timers;

    public OneShotTimerPageViewModel(
        OneShotTimerLifetimeManager oneShotTimerLifetimeManager,
        IOneShotTimerDialogService oneShotTimerDialogService
        )
    {
        _oneShotTimerLifetimeManager = oneShotTimerLifetimeManager;
        _oneShotTimerDialogService = oneShotTimerDialogService;
    }

    protected override void OnActivated()
    {
        Timers = _oneShotTimerLifetimeManager.Timers.ToReadOnlyReactiveCollection(x => new OneShotTimerViewModel(x, Messenger, DeleteTimer));
        base.OnActivated();
    }

    protected override void OnDeactivated()
    {
        _timers.Dispose();
        Timers = null;
        base.OnDeactivated();
    }

    [ObservableProperty]
    private bool _nowEditting;

    [RelayCommand]
    async Task EditTimer(OneShotTimerViewModel timerVM)
    {
        if (timerVM.RunningInfo.IsRunning) { return; }

        var result = await _oneShotTimerDialogService.ShowEditTimerAsync("OneShotTimerEditDialog_Title".Translate(), timerVM.Title, timerVM.Time, timerVM.RunningInfo.SoundSourceType, timerVM.RunningInfo.Parameter);
        if (result.IsConfirmed)
        {
            using (timerVM.RunningInfo.DeferUpdate())
            {
                timerVM.Title = result.Title;
                timerVM.Time = result.Time;
                timerVM.RunningInfo.SoundSourceType = result.SoundSourceType;
                timerVM.RunningInfo.Parameter = result.SoundParameter;
            }
        }
    }

    [RelayCommand]
    void DeleteTimer(OneShotTimerViewModel timerVM)
    {
        _oneShotTimerLifetimeManager.DeleteTimer(timerVM.RunningInfo);
    }
    

    [RelayCommand]
    void DeleteToggle()
    {
        NowEditting = !NowEditting;
        foreach (var timer in Timers)
        {
            timer.IsEditting = NowEditting;
        }
    }

    [RelayCommand]
    async Task AddTimer()
    {
        var result = await _oneShotTimerDialogService.ShowEditTimerAsync("OneShotTimerAddDialog_Title".Translate(), "", TimeSpan.FromMinutes(3), SoundSourceType.System,  WindowsNotificationSoundType.Default.ToString());
        if (result.IsConfirmed)
        {
            var timer = _oneShotTimerLifetimeManager.CreateTimer(result.Title, result.Time);

        }
    }
}

public sealed partial class OneShotTimerViewModel : ObservableObject
{
    public OneShotTimerViewModel(OneShotTimerRunningInfo runningInfo, IMessenger messenger, Action<OneShotTimerViewModel> onDeleteAction)
    {
        RunningInfo = runningInfo;
        _messenger = messenger;
        _onDeleteAction = onDeleteAction;
        _time = RunningInfo.Time;
        _title = RunningInfo.Title;
        _remainingTime = RunningInfo.RemainingTime;
        IsTimerActive = RunningInfo.Time != RunningInfo.RemainingTime;
    }

    private readonly IMessenger _messenger;
    private readonly Action<OneShotTimerViewModel> _onDeleteAction;

    [ObservableProperty]
    private TimeSpan _time;

    partial void OnTimeChanged(TimeSpan value)
    {
        RunningInfo.Time = value;
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

    public OneShotTimerRunningInfo RunningInfo { get; }

    public string ConvertTime(TimeSpan time)
    {
        return time.TrimMilliSeconds().ToString("c");
    }

    public string ConvertTime(TimeOnly? time)
    {
        return time?.ToString("T") ?? string.Empty;
    }

    public double ConvertToDouble(TimeSpan time)
    {
        return time.TotalSeconds;
    }

    public double ConvertToDouble(TimeOnly? time)
    {
        return time?.ToTimeSpan().TotalSeconds ?? 0.0d;
    }

#if DEBUG
    Stopwatch _sw = new Stopwatch();
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
            RunningInfo.StartTimer((remainingTime) => 
            {
                RemainingTime = remainingTime;
            }
            ,(info) =>
            {                
#if DEBUG
                _sw.Stop();
                Debug.WriteLine($"Time: {Time:T} / Real: {_sw.Elapsed:T}");
#endif
                // TODO: 画面上の通知動作を実行                
            });

            RemainingTime = RunningInfo.RemainingTime;
            EndTime = TimeOnly.FromDateTime(RunningInfo.StartTime + Time);
            IsTimerActive = true;
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
        IsTimerActive = RunningInfo.IsRunning;
    }

    bool CanExecuteRewindTimer()
    {
        return IsTimerActive;
    }

    [RelayCommand]
    void ShowWithCompactOverlay()
    {

    }

    [RelayCommand]
    void ShowWithFillWindow()
    {

    }

    [RelayCommand]
    void ShowWithDefault()
    {

    }

    [RelayCommand]
    void Delete()
    {
        _onDeleteAction(this);
    }
}
