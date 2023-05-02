using System;
using System.Threading.Tasks;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Views.Controls;

namespace VoiceOfClock.Contracts.Services;

public interface IOneShotTimerDialogService
{
    Task<OneShotTimerDialogResult> ShowEditTimerAsync(
        string dialogTitle, 
        string timerTitle,
        TimeSpan time,        
        SoundSourceType soundSourceType, 
        string soundParameter,
        TimeSelectBoxDisplayMode timeSelectBoxDisplayMode = TimeSelectBoxDisplayMode.Hours_Minutes_Seconds
        );
}

public sealed class OneShotTimerDialogResult
{
    public bool IsConfirmed { get; init; }
    public string Title { get; init; } = string.Empty;
    public TimeSpan Time { get; init; }
    public SoundSourceType SoundSourceType { get; init; }
    public string SoundParameter { get; init; } = string.Empty;
}