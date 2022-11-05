using CommunityToolkit.Mvvm.Messaging;
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
using VoiceOfClock.Models.Domain;
using Windows.Foundation.Collections;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace VoiceOfClock.UseCases;


static class OneShotTimerConstants
{
    public const int UpdateFPS = 6;
}

public sealed class OneShotTimerLifetimeManager : IApplicationLifeCycleAware
    , IRecipient<ActiveTimerCollectionRequestMessage>
    , IToastActivationAware
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IMessenger _messenger;
    private readonly OneShotTimerRepository _oneShotTimerRepository;
    private readonly OneShotTimerRunningRepository _oneShotTimerRunningRepository;

    public OneShotTimerLifetimeManager(
        IMessenger messenger,
        OneShotTimerRepository oneShotTimerRepository,
        OneShotTimerRunningRepository oneShotTimerRunningRepository
        )
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
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

        var timers = _oneShotTimerRepository.ReadAllItems().OrderBy(x => x.Order);
        foreach (var timer in timers)
        {
            var info = new OneShotTimerRunningInfo(timer, _oneShotTimerRepository, _oneShotTimerRunningRepository, _messenger);
            _timers.Add(info);
            info.OnTimesUp += RunningInfo_OnTimesUp;
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
        var runningInfo = new OneShotTimerRunningInfo(entity, _oneShotTimerRepository, _oneShotTimerRunningRepository, _messenger);
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
        CancellationTokenSource cts = new CancellationTokenSource();
        _playCancelMap.Add(runningInfo.EntityId, cts);
        CancellationToken ct = cts.Token;
        if (runningInfo.SoundSourceType == SoundSourceType.System)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await _messenger.Send(new PlaySystemSoundRequest(Enum.Parse<WindowsNotificationSoundType>(runningInfo.Parameter), ct));
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _playCancelMap.Remove(runningInfo.EntityId);
                    cts.Dispose();
                }
            });            
        }
        else if (runningInfo.SoundSourceType == SoundSourceType.Tts)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {

                    foreach (var i in Enumerable.Range(0, TimersToastNotificationConstants.VoiceNotificationRepeatCount))
                    {
                        if (i != 0)
                        {
                            await Task.Delay(TimersToastNotificationConstants.VoiceNotificationRepeatInterval, ct);
                        }

                        await _messenger.Send(new TextPlayVoiceRequest(runningInfo.Parameter, ct));
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _playCancelMap.Remove(runningInfo.EntityId);
                    cts.Dispose();
                }
            });
        }
        else if (runningInfo.SoundSourceType == SoundSourceType.TtsWithSSML)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    foreach (var i in Enumerable.Range(0, TimersToastNotificationConstants.VoiceNotificationRepeatCount))
                    {
                        if (i != 0)
                        {
                            await Task.Delay(TimersToastNotificationConstants.VoiceNotificationRepeatInterval, ct);
                        }

                        await _messenger.Send(new SsmlPlayVoiceRequest(runningInfo.Parameter, ct));
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _playCancelMap.Remove(runningInfo.EntityId);
                    cts.Dispose();
                }                
            });
        }

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
            return true;
        }

        return false;
    }
}
