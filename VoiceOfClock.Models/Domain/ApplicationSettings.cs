using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Infrastructure;

namespace VoiceOfClock.Models.Domain
{
    public sealed class ApplicationSettings : SettingsBase
    {
        public ApplicationSettings()
        {
            _themeName = Enum.Parse<ElementTheme>(Read(ElementTheme.Default.ToString(), nameof(Theme)));
        }

        private ElementTheme _themeName;
        public ElementTheme Theme 
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
    }
}
