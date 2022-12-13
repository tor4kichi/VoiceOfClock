using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using Windows.Services.Store;
using Windows.UI;
using WinRT.Interop;

namespace VoiceOfClock.Services;

public sealed class StoreLisenceService : IStoreLisenceService
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly StoreContext _context;
    private readonly ILisencePurchaseDialogService _lisencePurchaseConfirmDialogService;

    public StoreLisenceService(
        ILisencePurchaseDialogService lisencePurchaseConfirmDialogService
        )
    {
        _isActive = new ReactiveProperty<bool>();
        IsActive = _isActive.ToReadOnlyReactiveProperty();
        _isTrial = new ReactiveProperty<bool>();
        IsTrial = _isTrial.ToReadOnlyReactiveProperty();
        _nowUpdating = new ReactiveProperty<bool>();
        NowUpdating = _nowUpdating.ToReadOnlyReactiveProperty(true);
        _isTrialOwnedByThisUser = new ReactiveProperty<bool>();
        IsTrialOwnedByThisUser = _isTrialOwnedByThisUser.ToReadOnlyReactiveProperty();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _context = StoreContext.GetDefault();
        _context.OfflineLicensesChanged += _context_OfflineLicensesChanged;
        _lisencePurchaseConfirmDialogService = lisencePurchaseConfirmDialogService;
    }

    bool _initialized;
    public async ValueTask EnsureInitializeAsync()
    {
        if (_initialized is false)
        {
            try
            {
                await UpdateLisenceAsync();
            }
            catch { }

            _initialized = true;
        }
    }

    private async Task UpdateLisenceAsync()
    {
        _nowUpdating.Value = true;
        try
        {
            var appLisence = await _context.GetAppLicenseAsync();
            _isActive.Value = appLisence.IsActive;
            _isTrial.Value = appLisence.IsTrial;
            _isTrialOwnedByThisUser.Value = appLisence.IsTrialOwnedByThisUser;
        }
        finally
        {
            _nowUpdating.Value = false;
        }
    }

    public async Task<(bool IsSuccessed, Exception? error)> RequestPurchaiceLisenceAsync(string dialogTitle)
    {
        if (await _lisencePurchaseConfirmDialogService.ShowPurchaseMainProductConfirmDialogAsync(dialogTitle) is false)
        {
            return (false, null);
        }

        App.Current.InitializeWithWindow(_context);

        var appLisence = await _context.GetAppLicenseAsync();
        var result = await _context.RequestPurchaseAsync(appLisence.SkuStoreId);
        return (result.Status == StorePurchaseStatus.Succeeded, result.ExtendedError);
    }


    private ReactiveProperty<bool> _nowUpdating;
    public IReadOnlyReactiveProperty<bool> NowUpdating { get; }


    private ReactiveProperty<bool> _isActive;
    public IReadOnlyReactiveProperty<bool> IsActive { get; }

    private ReactiveProperty<bool> _isTrial;
    public IReadOnlyReactiveProperty<bool> IsTrial { get; }

    private ReactiveProperty<bool> _isTrialOwnedByThisUser;
    public IReadOnlyReactiveProperty<bool> IsTrialOwnedByThisUser { get; }


    private void _context_OfflineLicensesChanged(StoreContext sender, object args)
    {
        _ = UpdateLisenceAsync();
    }

}
