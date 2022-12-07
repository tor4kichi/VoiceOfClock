using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using I18NPortable;
using LiteDB;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Contract.Services;
using VoiceOfClock.Contract.UseCases;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Services.SoundPlayer;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;

namespace VoiceOfClock.UseCases;


static class OneShotTimerConstants
{
    public const int UpdateFPS = 6;
}

public sealed class OneShotTimerLifetimeManager 
    : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
    , IToastActivationAware
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly OneShotTimerRepository _oneShotTimerRepository;
    private readonly OneShotTimerRunningRepository _oneShotTimerRunningRepository;

    public OneShotTimerLifetimeManager(
        ISoundContentPlayerService soundContentPlayerService,
        OneShotTimerRepository oneShotTimerRepository,
        OneShotTimerRunningRepository oneShotTimerRunningRepository
        )
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _soundContentPlayerService = soundContentPlayerService;
        _oneShotTimerRepository = oneShotTimerRepository;
        _oneShotTimerRunningRepository = oneShotTimerRunningRepository;        
        _timers = new ObservableCollection<OneShotTimerRunningInfo>();
        Timers = new ReadOnlyObservableCollection<OneShotTimerRunningInfo>(_timers);
    }

    public ReadOnlyObservableCollection<OneShotTimerRunningInfo> Timers { get; }
    public ObservableCollection<OneShotTimerRunningInfo> _timers;

    void IApplicationLifeCycleAware.Initialize()
    {
        var timers = _oneShotTimerRepository.ReadAllItems().OrderBy(x => x.Order);
        foreach (var timer in timers)
        {
            var info = new OneShotTimerRunningInfo(timer, _oneShotTimerRepository, _oneShotTimerRunningRepository);
            _timers.Add(info);
            info.OnTimesUp += RunningInfo_OnTimesUp;
        }

        if (SystemInformation.Instance.IsFirstRun)
        {
            CreateTimer("OneShotTimer_TemporaryTitle".Translate(1), TimeSpan.FromMinutes(3), SoundSourceType.System, WindowsNotificationSoundType.Reminder.ToString());
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

    public OneShotTimerRunningInfo CreateTimer(string title, TimeSpan time, SoundSourceType soundSourceType, string soundParameter)
    {
        var entity = _oneShotTimerRepository.CreateItem(new OneShotTimerEntity() 
        {
            Title = title, 
            Time = time,
            SoundType = soundSourceType,
            SoundParameter = soundParameter,
            Order = int.MaxValue,            
        });
        var runningInfo = new OneShotTimerRunningInfo(entity, _oneShotTimerRepository, _oneShotTimerRunningRepository);
        _timers.Add(runningInfo);
        runningInfo.OnTimesUp += RunningInfo_OnTimesUp;

        // 並び順を確実に指定する
        foreach (var (timer, index) in _timers.Select((x, i) => (x, i)))
        {
            timer._entity.Order = index;
            _oneShotTimerRepository.UpdateItem(timer._entity);
        }

        return runningInfo;
    }

    public void DeleteTimer(OneShotTimerRunningInfo info)
    {
        _oneShotTimerRepository.DeleteItem(info.EntityId);
        _oneShotTimerRunningRepository.DeleteItem(info.EntityId);
        info.OnTimesUp -= RunningInfo_OnTimesUp;
        foreach (var remItem in _timers.Where(x => x.EntityId == info.EntityId).ToArray())
        {
            _timers.Remove(remItem);
        }
    }

    private readonly Dictionary<Guid, CancellationTokenSource> _playCancelMap = new();

    private void RunningInfo_OnTimesUp(object? sender, OneShotTimerRunningInfo runningInfo)
    {
        ShowOneShotTimerToastNotification(runningInfo);
        PlayTimerSound(runningInfo);
    }

    private async void PlayTimerSound(OneShotTimerRunningInfo runningInfo)
    {
        if (_playCancelMap.Remove(runningInfo.EntityId, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }
        CancellationTokenSource cts = new CancellationTokenSource();
        _playCancelMap.Add(runningInfo.EntityId, cts);
        CancellationToken ct = cts.Token;

        try
        {
            await _soundContentPlayerService.PlaySoundContentAsync(runningInfo.SoundSourceType, runningInfo.Parameter, cancellationToken: ct);

        }
        catch (OperationCanceledException) { }
        finally
        {
            _playCancelMap.Remove(runningInfo.EntityId);
            cts.Dispose();
        }
    }

    private static void ShowOneShotTimerToastNotification(OneShotTimerRunningInfo runningInfo)
    {
        var args = new ToastArguments()
        {
            { TimersToastNotificationConstants.ArgumentKey_Action, TimersToastNotificationConstants.ArgumentValue_OneShot },
            { TimersToastNotificationConstants.ArgumentKey_Confirmed },
            { TimersToastNotificationConstants.ArgumentKey_TimerId, runningInfo.EntityId.ToString() }
        };

        var tcb = new ToastContentBuilder();
        foreach (var arg in args)
        {
            tcb.AddArgument(arg.Key, arg.Value);
        }

        tcb.AddAudio(new Uri("ms-winsoundevent:Notification.Default", UriKind.RelativeOrAbsolute), silent: true)
            .AddText("OneShotTimer_ToastNotificationTitle".Translate())
            .AddAttributionText($"{runningInfo.Title}\n{"Time_Elapsed".Translate(runningInfo.Time.TranslateTimeSpan())}")
            .SetToastScenario(ToastScenario.Reminder)
            .AddButton("Close".Translate(), ToastActivationType.Background, args.ToString())
            .Show();
    }

    bool IToastActivationAware.ProcessToastActivation(ToastArguments args, ValueSet props)
    {
        if (!IToastActivationAware.IsContainAction(args, TimersToastNotificationConstants.ArgumentValue_OneShot)) { return false; }

        if (args.TryGetValue(TimersToastNotificationConstants.ArgumentKey_TimerId, out string timerId))
        {
            Guid entityId = Guid.Parse(timerId);
            if (_playCancelMap.Remove(entityId, out var cts))
            {
                cts.Cancel();
            }

            var timerRunningInfo = _timers.FirstOrDefault(x => x.EntityId == entityId);
            if (timerRunningInfo != null)
            {
                timerRunningInfo.RewindTimer();
            }

            return true;
        }

        return false;
    }
}
