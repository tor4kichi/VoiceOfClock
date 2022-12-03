using CommunityToolkit.Mvvm.ComponentModel;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Models.Domain;

namespace VoiceOfClock.ViewModels
{
    
    public sealed partial class SoundSelectionItemViewModel : ObservableObject
    {
        public SoundSourceType SoundSourceType { get; init; }

        [ObservableProperty]
        private string _soundContent = string.Empty;        

        public string? Label { get; init; }
    }
}
