using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI.Helpers;
using DryIoc;
using I18NPortable;
using LiteDB;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VoiceOfClock.Models.Domain;
using VoiceOfClock.UseCases;
using VoiceOfClock.ViewModels;
using VoiceOfClock.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
        container.RegisterInstance<ILiteDatabase>(new LiteDatabase($"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, "user.db")}; Async=false;"));

        container.Register<TimerSettings>(reuse: new SingletonReuse());
        container.Register<ApplicationSettings>(reuse: new SingletonReuse());

        container.Register<TimerLifetimeManager>(reuse: new SingletonReuse());
        container.Register<OneShotTimerLifetimeManager>(reuse: new SingletonReuse());
        container.Register<SystemSoundPlayer>(reuse: new SingletonReuse());
        container.Register<VoicePlayer>(reuse: new SingletonReuse());

        container.RegisterMapping<IApplicationLifeCycleAware, TimerLifetimeManager>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IApplicationLifeCycleAware, OneShotTimerLifetimeManager>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IApplicationLifeCycleAware, VoicePlayer>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IApplicationLifeCycleAware, SystemSoundPlayer>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);

        container.Register<SettingsPageViewModel>();
        container.Register<PeriodicTimerPageViewModel>();
        container.Register<OneShotTimerPageViewModel>();
    }

    private static void RegisterTypes(Container container)
    {
        container.RegisterInstance<IMessenger>(WeakReferenceMessenger.Default);
        container.Register<IPeriodicTimerDialogService, PeriodicTimerEditDialogService>();
        container.Register<IOneShotTimerDialogService, OneShotTimerEditDialogService>();
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
    }


    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {        
        if (_window is null)
        {
            // 言語の指定
            if (Container.Resolve<ApplicationSettings>() is not null and var appSettings
                && I18N.Current.Languages.FirstOrDefault(x => x.Locale == appSettings.DisplayLanguage) is not null and var language)
            {
                I18N.Current.Locale = language.Locale;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(language.Locale);
            }

            // ライフサイクル対応のインスタンスに対して初期化を実行
            foreach (var item in _lifeCycleAwareInstances)
            {
                item.Initialize();
            }

            _window = new MainWindow();
            _window.Activate();
            _window.NavigateFirstPage(args.Arguments);
        }
    }

    internal void OnRedirectActiavated(AppActivationArguments args)
    {

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
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(target, hWnd);        
    }    

    public void InitializeDialog(ContentDialog dialog)
    {
        dialog.XamlRoot = WindowContent.XamlRoot;
        dialog.RequestedTheme = WindowContentRequestedTheme;
    }
}
