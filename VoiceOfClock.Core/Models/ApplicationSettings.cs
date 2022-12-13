using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Infrastructure;

namespace VoiceOfClock.Core.Models;

public enum ApplicationTheme
{
    Default,
    Light,
    Dark,
}

public sealed class ApplicationSettings : SettingsBase
{
    public ApplicationSettings()
    {
        _themeName = Enum.Parse<ApplicationTheme>(Read(ApplicationTheme.Default.ToString(), nameof(Theme))!);
        _displayLanguage = Read(CultureInfo.CurrentCulture.Name, nameof(DisplayLanguage))!;
        _dontShowWindowCloseConfirmDialog = Read(false, nameof(DontShowWindowCloseConfirmDialog));
    }

    private ApplicationTheme _themeName;
    public ApplicationTheme Theme 
    {
        get => _themeName;
        set
        {                
            if (_themeName != value)
            {
                OnPropertyChanging();
                _themeName = value;
                Save(_themeName.ToString());
                OnPropertyChanged();
            }
        }
    }

    private string _displayLanguage;
    public string DisplayLanguage
    {
        get => _displayLanguage;
        set => SetProperty(ref _displayLanguage, value);
    }

    private bool _dontShowWindowCloseConfirmDialog;
    public bool DontShowWindowCloseConfirmDialog
    {
        get => _dontShowWindowCloseConfirmDialog;
        set => SetProperty(ref _dontShowWindowCloseConfirmDialog, value);
    }

}
