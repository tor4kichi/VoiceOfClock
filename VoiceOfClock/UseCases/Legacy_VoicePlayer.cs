using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech;
using System.Speech.Synthesis;
using System.Reactive.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System.Globalization;

namespace VoiceOfClock.UseCases;

public class Legacy_VoicePlayer : IApplicationLifeCycleAware,
    IRecipient<TimeOfDayPlayVoiceRequest>
{
    private readonly IMessenger _messenger;
    private readonly TranslationProcesser _translationProcesser;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SpeechSynthesizer _speechSynthesiser;

    public Legacy_VoicePlayer(
        IMessenger messenger,
        TranslationProcesser translationProcesser
        )
    {
        _messenger = messenger;
        _translationProcesser = translationProcesser;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();        
        _speechSynthesiser = new SpeechSynthesizer();
    }


    void IApplicationLifeCycleAware.Initialize()
    {
        _messenger.RegisterAll(this);
    }

    void IApplicationLifeCycleAware.Resuming()
    {

    }

    void IApplicationLifeCycleAware.Suspending()
    {

    }


    void IRecipient<TimeOfDayPlayVoiceRequest>.Receive(TimeOfDayPlayVoiceRequest message)
    {
        var data = message.Data;
        string speechData = _translationProcesser.TranslateTimeOfDay(data.Time);
        message.Reply(_dispatcherQueue.EnqueueAsync(async () =>
        {
            var speakCompletedObservable = Observable.FromEventPattern<SpeakCompletedEventArgs>(
                h => _speechSynthesiser.SpeakCompleted += h,
                h => _speechSynthesiser.SpeakCompleted -= h
                ).Take(1);

            var prompt = _speechSynthesiser.SpeakAsync(speechData);

            await speakCompletedObservable;

            return new PlayVoiceResult();
        }));
    }

    //async ValueTask PlayOrAddQueueMediaSource(MediaSource source)
    //{
    //    var audioGraphResult = await AudioGraph.CreateAsync(new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Alerts));
    //    if (audioGraphResult.Status == AudioGraphCreationStatus.Success)
    //    {
    //        TaskCompletionSource<long> tcs = new TaskCompletionSource<long>();
    //        using var graph = audioGraphResult.Graph;
    //        var inputNodeResult = await graph.CreateMediaSourceAudioInputNodeAsync(source);
    //        var outputNodeResult = await graph.CreateDeviceOutputNodeAsync();

    //        inputNodeResult.Node.AddOutgoingConnection(outputNodeResult.DeviceOutputNode);
    //        inputNodeResult.Node.MediaSourceCompleted += (s, e) => { tcs.SetResult(0); };

    //        graph.Start();

    //        await tcs.Task;
    //    }
    //    else
    //    {

    //    }
    //}

}

