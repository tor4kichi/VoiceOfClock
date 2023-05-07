using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI.Helpers;
using DryIoc;
using I18NPortable;
using LiteDB;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VoiceOfClock.Contracts.Services;
using VoiceOfClock.Core.Contracts.Models;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Infrastructure;
using VoiceOfClock.Core.Migrate;
using VoiceOfClock.Core.Models;
using VoiceOfClock.Core.Models.Timers;
using VoiceOfClock.Core.Services;
using VoiceOfClock.Services;
using VoiceOfClock.Services.Dialogs;
using VoiceOfClock.Services.SoundPlayer;
using VoiceOfClock.ViewModels;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Services.Store;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VoiceOfClock;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public new static App Current => (App)Application.Current;

    public Container Container { get; }    

    private readonly ImmutableArray<IApplicationLifeCycleAware> _lifeCycleAwareInstances;

    public DispatcherQueue DispatcherQueue { get; }

    private static Container ConfigureService()
    {
        var rules = Rules.Default
            .WithAutoConcreteTypeResolution()
            .With(Made.Of(FactoryMethod.ConstructorWithResolvableArguments))
            .WithoutThrowOnRegisteringDisposableTransient()
            .WithFuncAndLazyWithoutRegistration()
            .WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.Replace);

        var container = new Container(rules);

        RegisterRequiredTypes(container);
        RegisterTypes(container);

        Ioc.Default.ConfigureServices(container);
        return container;
    }

    private static void RegisterRequiredTypes(Container container)
    {
        string dbFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "user.db");
        try
        {
            container.RegisterInstance<ILiteDatabase>(new LiteDatabase($"Filename={dbFilePath}; Async=false; Password=eNmaHYFAMksZ"));
        }
        catch
        {
            if (File.Exists(dbFilePath))
            {
                File.Move(dbFilePath, Path.ChangeExtension(dbFilePath, ".db.backup"), overwrite: true);
                File.Delete(dbFilePath);
            }
            
            container.RegisterInstance<ILiteDatabase>(new LiteDatabase($"Filename={dbFilePath}; Async=false; Password=eNmaHYFAMksZ"));
        }

        // Core.Domain
        container.Register<TimerSettings>(reuse: new SingletonReuse());
        container.Register<ApplicationSettings>(reuse: new SingletonReuse());

        container.RegisterDelegate<MediaPlayer>(() => 
        {
            var mp = new MediaPlayer();
            mp.CommandManager.IsEnabled = false;
            mp.SystemMediaTransportControls.IsEnabled = false;
            return mp;
        });

        // Core.Services
        container.RegisterInstance<IStorageHelper>(new BytesApplicationDataStorageHelper(ApplicationData.Current, new BinaryJsonObjectSerializer()));
        container.Register<TimeTriggerService>(reuse: new SingletonReuse());
        container.Register<ToastNotificationService>(reuse: new SingletonReuse());

        // UseCases
        container.Register<PeriodicTimerLifetimeManager>(reuse: new SingletonReuse());
        container.Register<OneShotTimerLifetimeManager>(reuse: new SingletonReuse());
        container.Register<AlarmTimerLifetimeManager>(reuse: new SingletonReuse());

        // Services
        container.Register<IStoreLisenceService, StoreLisenceService >(reuse: new SingletonReuse());
        container.Register<ISoundContentPlayerService, SoundContentPlayerService>(reuse: new SingletonReuse());
        container.Register<ISingleTimeTrigger, DispatcherQueueTimerTimeTrigger>(reuse: new SingletonReuse());
        container.Register<ILocalizationService, LocalizationService>();
        container.Register<AddSampleTimersOnFirstRunService>();

        container.RegisterMapping<IApplicationLifeCycleAware, AddSampleTimersOnFirstRunService>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IApplicationLifeCycleAware, PeriodicTimerLifetimeManager>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IApplicationLifeCycleAware, OneShotTimerLifetimeManager>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IApplicationLifeCycleAware, AlarmTimerLifetimeManager>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IApplicationLifeCycleAware, TimeTriggerService>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);

        container.RegisterMapping<ITimeTriggerService, TimeTriggerService>();

        container.RegisterMapping<IToastNotificationService, ToastNotificationService>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IToastActivationAware, ToastNotificationService>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        
        container.Register<SettingsPageViewModel>();
        container.Register<PeriodicTimerPageViewModel>();
        container.Register<OneShotTimerPageViewModel>();
        container.Register<AlarmTimerPageViewModel>();
    }

    private static void RegisterTypes(Container container)
    {
        container.RegisterInstance<IMessenger>(WeakReferenceMessenger.Default);
        container.Register<IPeriodicTimerDialogService, PeriodicTimerEditDialogService>();
        container.Register<IOneShotTimerDialogService, OneShotTimerEditDialogService>();
        container.Register<IAlarmTimerDialogService, AlarmTimerEditDialogService>();
        container.Register<ILisencePurchaseDialogService, LisencePurchaseDialogService>();
        container.Register<IAudioSoundSourceDialogService, AudioSoundSourceDialogService>();
        container.Register<ITimeZoneDialogService, TimeZoneDialogService>();        
    }    

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        Container = ConfigureService();

        _lifeCycleAwareInstances = Container.ResolveMany<IApplicationLifeCycleAware>(typeof(IApplicationLifeCycleAware), ResolveManyBehavior.AsFixedArray).ToImmutableArray();

        I18N.Current.Init(GetType().GetAssembly())
            .SetFallbackLocale("en-US")
            .SetNotFoundSymbol("🍣")               
            ;

        DispatcherQueue = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        // 言語の指定
        if (Container.Resolve<ApplicationSettings>() is not null and var appSettings
            && I18N.Current.Languages.FirstOrDefault(x => x.Locale == appSettings.DisplayLanguage) is not null and var language)
        {
            I18N.Current.Locale = language.Locale;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(language.Locale);
        }

        // バージョン間マイグレーションを実行
        {
            Version prevVersion = new Version(SystemInformation.Instance.PreviousVersionInstalled.ToFormattedString());
            Version currentVersion = new Version(SystemInformation.Instance.ApplicationVersion.ToFormattedString());
            Container.Resolve<MigrationService>().Migrate(currentVersion, prevVersion);
        }

        // ライフサイクル対応のインスタンスに対して初期化を実行
        foreach (var item in _lifeCycleAwareInstances)
        {
            item.Initialize();
        }        
    }


    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // Register for toast activation. Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
        ToastNotificationManagerCompat.OnActivated -= ToastNotificationManagerCompat_OnActivated;
        ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

        // If we weren't launched by a toast, launch our window like normal.
        // Otherwise if launched by a toast, our OnActivated callback will be triggered
        if (!ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            LaunchAndBringToForegroundIfNeeded(args);
        }        
    }


    private async void LaunchAndBringToForegroundIfNeeded(Microsoft.UI.Xaml.LaunchActivatedEventArgs? args = null)
    {
        if (_window is null)
        {            
            _window = new MainWindow();
            _window.NavigateFirstPage();
            _window.Activate();            

            // Additionally we show using our helper, since if activated via a toast, it doesn't
            // activate the window correctly
            WindowHelper.ShowWindow(_window);

            if (args != null)
            {
                SystemInformation.Instance.TrackAppUse(args.UWPLaunchActivatedEventArgs, _window.Content.XamlRoot);

                if (PurchaseItemsConstants.IsTrialLimitationEnabled
                    && SystemInformation.Instance.IsFirstRun is false 
                    && SystemInformation.Instance.IsAppUpdated                    
                    )
                {
                    var storeLisenceService = Ioc.Default.GetRequiredService<StoreLisenceService>();
                    await storeLisenceService.EnsureInitializeAsync();
                    if (storeLisenceService.IsTrial.Value && !storeLisenceService.IsTrialOwnedByThisUser.Value)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2))
                        .ContinueWith(_ =>
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                _ = storeLisenceService.RequestPurchaiceLisenceAsync("PurchaseDialog_TitleOnRecommended".Translate());
                            });
                        });
                    }                                    
                }
            }            
        }
        else
        {
            WindowHelper.ShowWindow(_window);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:未使用のパラメーターを削除します", Justification = "<保留中>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:メンバーを static に設定します", Justification = "<保留中>")]
    internal void OnRedirectActiavated(AppActivationArguments args)
    {
        LaunchAndBringToForegroundIfNeeded();
    }

    private MainWindow? _window;    

    private UIElement WindowContent => _window is not null ? _window.Content : throw new NullReferenceException();

    public ElementTheme WindowContentRequestedTheme
    {
        get => (WindowContent as FrameworkElement)!.RequestedTheme;
        set => (WindowContent as FrameworkElement)!.RequestedTheme = value;
    }

    public void InitializeWithWindow(object target)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        WinRT.Interop.InitializeWithWindow.Initialize(target, hWnd);        
    }    

    public void InitializeDialog(ContentDialog dialog)
    {        
        dialog.XamlRoot = WindowContent.XamlRoot;
        dialog.RequestedTheme = WindowContentRequestedTheme;
    }

    private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        // Use the dispatcher from the window if present, otherwise the app dispatcher
        var dispatcherQueue = _window?.DispatcherQueue ?? DispatcherQueue;

        dispatcherQueue.TryEnqueue(delegate
        {
            var args = ToastArguments.Parse(e.Argument);
            var toastProcessers = Container.ResolveMany<IToastActivationAware>(typeof(IToastActivationAware), ResolveManyBehavior.AsFixedArray).ToImmutableArray();
            foreach (var processer in toastProcessers)
            {
                if (processer.ProcessToastActivation(args, e.UserInput))                
                {
                    break;
                }
            }

            // If the UI app isn't open
            if (_window == null)
            {
                // Close since we're done
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                //LaunchAndBringToForegroundIfNeeded();
            }
        });
    }

    private static class WindowHelper
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void ShowWindow(Window window)
        {
            // Bring the window to the foreground... first get the window handle...
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Restore window if minimized... requires DLL import above
            ShowWindow(hwnd, 0x00000009);

            // And call SetForegroundWindow... requires DLL import above
            SetForegroundWindow(hwnd);
        }
    }
}

