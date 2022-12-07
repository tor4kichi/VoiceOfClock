using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using I18NPortable;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Contract.Services;
using VoiceOfClock.Contract.UseCases;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.Services.SoundPlayer;
using Windows.Foundation.Collections;

namespace VoiceOfClock.UseCases;

public sealed partial class AlarmTimerLifetimeManager 
    : IApplicationLifeCycleAware
    , IToastActivationAware
{
    private readonly DispatcherQueue _dispatcherQueue;    
    private readonly ISoundContentPlayerService _soundContentPlayerService;
    private readonly AlarmTimerRepository _alarmTimerRepository;
    
    private readonly ObservableCollection<AlarmTimerRunningInfo> _timers;
    public ReadOnlyObservableCollection<AlarmTimerRunningInfo> Timers { get; }

    public AlarmTimerLifetimeManager(        
        ISoundContentPlayerService soundContentPlayerService       
        , AlarmTimerRepository alarmTimerRepository
        )
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _soundContentPlayerService = soundContentPlayerService;
        _alarmTimerRepository = alarmTimerRepository;        
        _timers = new ObservableCollection<AlarmTimerRunningInfo>(_alarmTimerRepository.ReadAllItems().Select(ToRunningInfo));
        Timers = new(_timers);
    }

    AlarmTimerRunningInfo ToRunningInfo(AlarmTimerEntity entity)
    {
        return new AlarmTimerRunningInfo(entity, _alarmTimerRepository, _dispatcherQueue, OnAlarmTrigger);
    }

    private readonly Dictionary<Guid, CancellationTokenSource> _playCancelMap = new();

    async void OnAlarmTrigger(AlarmTimerRunningInfo runningInfo)
    {
        ShowAlarmToastNotification(runningInfo);
        PlayTimerSound(runningInfo);
    }

    private async void PlayTimerSound(AlarmTimerRunningInfo runningInfo)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        _playCancelMap.Add(runningInfo._entity.Id, cts);
        CancellationToken ct = cts.Token;
        try
        {
            await _soundContentPlayerService.PlaySoundContentAsync(runningInfo.SoundSourceType, runningInfo.SoundContent, cancellationToken: ct);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _playCancelMap.Remove(runningInfo._entity.Id);
            cts.Dispose();
        }
    }

    private static void ShowAlarmToastNotification(AlarmTimerRunningInfo runningInfo)
    {
        var stopToastArgs = new ToastArguments()
        {
            { TimersToastNotificationConstants.ArgumentKey_Action, TimersToastNotificationConstants.ArgumentValue_Alarm },
            { TimersToastNotificationConstants.ArgumentKey_SnoozeStop },
            { TimersToastNotificationConstants.ArgumentKey_TimerId, runningInfo._entity.Id.ToString() }
        };

        var againToastArgs = new ToastArguments()
        {
            { TimersToastNotificationConstants.ArgumentKey_Action, TimersToastNotificationConstants.ArgumentValue_Alarm },
            { TimersToastNotificationConstants.ArgumentKey_SnoozeAgain },
            { TimersToastNotificationConstants.ArgumentKey_TimerId, runningInfo._entity.Id.ToString() }
        };

        if (TimeSpan.Zero < runningInfo.Snooze)
        {
            var tcb = new ToastContentBuilder();
            foreach (var kvp in againToastArgs)
            {
                tcb.AddArgument(kvp.Key, kvp.Value);
            }

            string defaultSelectComboBoxId = runningInfo.Snooze.Value.ToString();
            tcb.AddText("AlarmTimer_ToastNotificationTitle".Translate())
                .AddAttributionText($"{runningInfo.Title}\n{runningInfo.TargetTime.ToShortTimeString()}\n\n{"AlarmTimer_ToastNotificationSnoozeTimeDescription".Translate()}")
                .AddComboBox(
                    TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id
                    , defaultSelectComboBoxId
                    , AlarmTimerConstants.SnoozeTimes.Select(x => (comboBoxItemKey: x.ToString(), comboBoxItemContent: "AlarmTimer_SnoozeWithMinutes".Translate(x.TotalMinutes))).ToArray()
                    )
                .AddButton("AlarmTimer_Snooze".Translate(), ToastActivationType.Background, againToastArgs.ToString())
                .AddButton("Close".Translate(), ToastActivationType.Background, stopToastArgs.ToString())
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default", UriKind.RelativeOrAbsolute), silent: true)
                ;
            tcb.Show();
        }
        else
        {
            var tcb = new ToastContentBuilder();
            foreach (var kvp in stopToastArgs)
            {
                tcb.AddArgument(kvp.Key, kvp.Value);
            }

            tcb.AddText("AlarmTimer_ToastNotificationTitle".Translate())
                .AddAttributionText($"{runningInfo.Title}\n{runningInfo.TargetTime.ToShortTimeString()}")
                .AddButton("Close".Translate(), ToastActivationType.Background, stopToastArgs.ToString())
                .AddAudio(new Uri("ms-winsoundevent:Notification.Default", UriKind.RelativeOrAbsolute), silent: true)
                ;
            tcb.Show();
        }
    }

    public bool ProcessToastActivation(ToastArguments args, ValueSet props)
    {
        if (!IToastActivationAware.IsContainAction(args, TimersToastNotificationConstants.ArgumentValue_Alarm)) { return false; }

        string timerId = args.Get(TimersToastNotificationConstants.ArgumentKey_TimerId);
        Guid entityId = Guid.Parse(timerId);
        AlarmTimerRunningInfo? timer = Timers.FirstOrDefault(x => x._entity.Id == entityId);        
        if (timer is null) 
        {
            return false; 
        }
        else if (args.Contains(TimersToastNotificationConstants.ArgumentKey_SnoozeStop))
        {
            timer.AlarmChecked();
            if (_playCancelMap.Remove(timer._entity.Id, out var cts))
            {
                cts.Cancel();
            }
            return true;
        }
        else if (args.Contains(TimersToastNotificationConstants.ArgumentKey_SnoozeAgain))
        {
            TimeSpan snooze = TimeSpan.Parse((string)props[TimersToastNotificationConstants.PropsKey_SnoozeTimeComboBox_Id]);
            timer.AlarmChecked(snooze);
            if (_playCancelMap.Remove(timer._entity.Id, out var cts))
            {
                cts.Cancel();
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    void IApplicationLifeCycleAware.Initialize()
    {
        if (SystemInformation.Instance.IsFirstRun)
        {
            CreateAlarmTimer("AlarmTimer_TemporaryTitle".Translate(1), TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), Enum.GetValues<DayOfWeek>(), null, SoundSourceType.System, WindowsNotificationSoundType.Reminder.ToString(), isEnabled: false);
        }
    }

    void IApplicationLifeCycleAware.Resuming()
    {
        
    }

    void IApplicationLifeCycleAware.Suspending()
    {
        
    }    

    public AlarmTimerRunningInfo CreateAlarmTimer(string title, TimeOnly timeOfDay, DayOfWeek[] enabledDayOfWeeks, TimeSpan? snoozeTime, SoundSourceType soundSourceType, string soundContent, bool isEnabled = true)
    {
        AlarmTimerEntity newEntity = _alarmTimerRepository.CreateItem(new AlarmTimerEntity
        {
            Title = title,
            TimeOfDay = timeOfDay,
            EnabledDayOfWeeks = enabledDayOfWeeks,
            Snooze = snoozeTime,
            SoundSourceType = soundSourceType,
            SoundContent = soundContent,            
            IsEnabled = isEnabled,
        });

        AlarmTimerRunningInfo newTimerRunningInfo = ToRunningInfo(newEntity);
        _timers.Add(newTimerRunningInfo);

        // 並び順を確実に指定する
        foreach (var (timer, index) in _timers.Select((x, i) => (x, i)))
        {
            timer._entity.Order = index;
            _alarmTimerRepository.UpdateItem(timer._entity);
        }

        return newTimerRunningInfo;
    }

    public bool DeleteAlarmTimer(AlarmTimerRunningInfo runningInfo)
    {
        _timers.Remove(runningInfo);

        return _alarmTimerRepository.DeleteItem(runningInfo._entity.Id);
    }
}
