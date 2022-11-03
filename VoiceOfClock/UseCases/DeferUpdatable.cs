using CommunityToolkit.Diagnostics;
using System;
using System.Reactive.Disposables;

namespace VoiceOfClock.UseCases;

public abstract class DeferUpdatable
{
    public IDisposable DeferUpdate()
    {
        Guard.IsFalse(NowDeferUpdateRequested, nameof(NowDeferUpdateRequested));

        NowDeferUpdateRequested = true;
        return Disposable.Create(OnDeferUpdate_Internal);
    }

    bool _nowDeferUpdateRequested;
    protected bool NowDeferUpdateRequested
    {
        get => _nowDeferUpdateRequested;
        private set => _nowDeferUpdateRequested = value;
    }

    private void OnDeferUpdate_Internal()
    {
        NowDeferUpdateRequested = false;
        OnDeferUpdate();
    }
    protected abstract void OnDeferUpdate();
}
