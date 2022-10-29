using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock;
using WinRT;

namespace VoiceOfClock;

// ref@ https://blog.shibayan.jp/entry/20220510/1652191991

public abstract class SystemBackdropWindow : Window
{
    protected SystemBackdropWindow()
    {
        Activated += Window_Activated;
        Closed += Window_Closed;
    }

    private readonly WindowsSystemDispatcherQueueHelper _wsdqHelper = new();

    private SystemBackdropConfiguration? _configurationSource;
    private ISystemBackdropControllerWithTargets? _systemBackdropController;

    protected bool TrySetSystemBackdrop()
    {
        if (MicaController.IsSupported())
        {
            SetSystemBackdropController<MicaController>();

            return true;
        }

        if (DesktopAcrylicController.IsSupported())
        {
            SetSystemBackdropController<DesktopAcrylicController>();

            return true;
        }

        return false;
    }

    private void SetSystemBackdropController<TSystemBackdropController>() where TSystemBackdropController : ISystemBackdropControllerWithTargets, new()
    {
        _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

        _configurationSource = new SystemBackdropConfiguration
        {
            IsInputActive = true,
            Theme = ((FrameworkElement)Content).ActualTheme switch
            {
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                ElementTheme.Light => SystemBackdropTheme.Light,
                ElementTheme.Default => SystemBackdropTheme.Default,
                _ => throw new ArgumentOutOfRangeException()
            }
        };

        _systemBackdropController = new TSystemBackdropController();

        _systemBackdropController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
        _systemBackdropController.SetSystemBackdropConfiguration(_configurationSource);
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_configurationSource is not null)
        {
            _configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        if (_systemBackdropController is not null)
        {
            _systemBackdropController.Dispose();
            _systemBackdropController = null;
        }

        Activated -= Window_Activated;

        _configurationSource = null;
    }
}