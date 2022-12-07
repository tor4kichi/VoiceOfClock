using Reactive.Bindings;
using System;
using System.Threading.Tasks;

namespace VoiceOfClock.Contract.Services;

public interface IStoreLisenceService
{
    IReadOnlyReactiveProperty<bool> IsActive { get; }
    IReadOnlyReactiveProperty<bool> IsTrial { get; }
    IReadOnlyReactiveProperty<bool> IsTrialOwnedByThisUser { get; }
    IReadOnlyReactiveProperty<bool> NowUpdating { get; }

    ValueTask EnsureInitializeAsync();
    Task<(bool IsSuccessed, Exception? error)> RequestPurchaiceLisenceAsync(string dialogTitle);
}