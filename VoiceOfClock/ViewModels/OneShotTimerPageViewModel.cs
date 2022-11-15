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
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Services;
using VoiceOfClock.UseCases;

namespace VoiceOfClock.ViewModels;

public interface IOneShotTimerDialogService
{
    Task<OneShotTimerDialogResult> ShowEditTimerAsync(string dialogTitle, string timerTitle,TimeSpan time, SoundSourceType soundSourceType, string soundParameter);
}

public sealed class OneShotTimerDialogResult
{
    public bool IsConfirmed { get; init; }
    public string Title { get; init; } = string.Empty;
    public TimeSpan Time { get; init; }
    public SoundSourceType SoundSourceType { get; init; }
    public string SoundParameter { get; init; } = string.Empty;
}

public sealed partial class OneShotTimerPageViewModel : ObservableRecipient
{
    private readonly OneShotTimerLifetimeManager _oneShotTimerLifetimeManager;
    private readonly IOneShotTimerDialogService _oneShotTimerDialogService;
    private readonly StoreLisenceService _storeLisenceService;

    [ObservableProperty]
    private ReadOnlyReactiveCollection<OneShotTimerViewModel>? _timers;

    public OneShotTimerPageViewModel(
        OneShotTimerLifetimeManager oneShotTimerLifetimeManager
        , IOneShotTimerDialogService oneShotTimerDialogService
        , StoreLisenceService storeLisenceService
        )
    {
        _oneShotTimerLifetimeManager = oneShotTimerLifetimeManager;
        _oneShotTimerDialogService = oneShotTimerDialogService;
        _storeLisenceService = storeLisenceService;
    }

    protected override void OnActivated()
    {
        Timers = _oneShotTimerLifetimeManager.Timers.ToReadOnlyReactiveCollection(x => new OneShotTimerViewModel(x, Messenger, DeleteTimer));
        base.OnActivated();
    }

    protected override void OnDeactivated()
    {
        _timers?.Dispose();
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
        foreach (var timer in Timers ?? Enumerable.Empty<OneShotTimerViewModel>())
        {
            timer.IsEditting = NowEditting;
        }
    }

    [RelayCommand]
    async Task AddTimer()
    {
        if (PurchaseItemsConstants.IsTrialLimitationEnabled)
        {
            await _storeLisenceService.EnsureInitializeAsync();
            if (_storeLisenceService.IsTrial.Value && _oneShotTimerLifetimeManager.Timers.Count >= PurchaseItemsConstants.Trial_TimersLimitationCount)
            {
                var (isSuccess, error) = await _storeLisenceService.RequestPurchaiceLisenceAsync("PurchaseDialog_TitleOnInteractFromUser".Translate());
                if (!isSuccess)
                {
                    return;
                }
            }
        }

        var result = await _oneShotTimerDialogService.ShowEditTimerAsync("OneShotTimerAddDialog_Title".Translate(), "", TimeSpan.FromMinutes(3), SoundSourceType.System,  WindowsNotificationSoundType.Default.ToString());
        if (result.IsConfirmed)
        {
            _oneShotTimerLifetimeManager.CreateTimer(result.Title, result.Time, result.SoundSourceType, result.SoundParameter);
        }
    }
}
