using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;

public partial class PeriodicTimerViewModel : ObservableObject
{
    public PeriodicTimerRunningInfo PeriodicTimerRunningInfo { get; }

    public PeriodicTimerViewModel(PeriodicTimerRunningInfo timerInfo, ICommand deleteCommand)
    {
        PeriodicTimerRunningInfo = timerInfo;
        DeleteCommand = deleteCommand;

        _isEnabled = PeriodicTimerRunningInfo.IsEnabled;
        _intervalTime = PeriodicTimerRunningInfo.IntervalTime;
        _startTime = PeriodicTimerRunningInfo.StartTime;
        _endTime = PeriodicTimerRunningInfo.EndTime;
        _title = PeriodicTimerRunningInfo.Title;
    }

    public void RefrectValue()
    {
        IsEnabled = PeriodicTimerRunningInfo.IsEnabled;
        IntervalTime = PeriodicTimerRunningInfo.IntervalTime;
        StartTime = PeriodicTimerRunningInfo.StartTime;
        EndTime = PeriodicTimerRunningInfo.EndTime;
        Title = PeriodicTimerRunningInfo.Title;
    }

    public bool IsRemovable => PeriodicTimerRunningInfo.IsInstantTimer is false;

    [ObservableProperty]
    private bool _isEditting;

    [ObservableProperty]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {        
        if (IsRemovable) { return; }
        PeriodicTimerRunningInfo.IsEnabled = value;
    }


    [ObservableProperty]
    private TimeSpan _intervalTime;

    [ObservableProperty]
    private TimeSpan _startTime;

    [ObservableProperty]
    private TimeSpan _endTime;

    [ObservableProperty]
    private string _title;

    public ICommand DeleteCommand { get; }


    public string LocalizeTime(TimeSpan timeSpan)
    {
        return "Time_Hours_Minutes".Translate(timeSpan.Minutes, timeSpan.Hours);
    }

    public string LocalizeIntervalTime(TimeSpan timeSpan)
    {
        return "IntervalTime_PerMinutes".Translate(timeSpan.TotalMinutes);
    }
}
