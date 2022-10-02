﻿using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using DryIoc;
using I18NPortable;
using LiteDB;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VoiceOfClock.UseCases;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

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

        container.Register<TimerLifetimeManager>(reuse: new SingletonReuse());
        container.Register<UWP_VoicePlayer>(reuse: new SingletonReuse());
        //container.Register<Legacy_VoicePlayer>(reuse: new SingletonReuse());

        container.RegisterMapping<IApplicationLifeCycleAware, TimerLifetimeManager>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<IApplicationLifeCycleAware, UWP_VoicePlayer>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        //container.RegisterMapping<IApplicationLifeCycleAware, Legacy_VoicePlayer>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
    }

    private static void RegisterTypes(Container container)
    {
        container.RegisterInstance<IMessenger>(WeakReferenceMessenger.Default);
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
        // ライフサイクル対応のインスタンスに対して初期化を実行
        foreach (var item in _lifeCycleAwareInstances)
        {
            item.Initialize();
        }

        _window = new MainWindow();        
        _window.Activate();

        _window.NavigateFirstPage(args.Arguments);
    }

    private MainWindow _window;
}