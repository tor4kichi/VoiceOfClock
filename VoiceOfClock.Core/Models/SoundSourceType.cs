﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Core.Models;

public enum SoundSourceType
{
    System,
    Tts,
    TtsWithSSML,
    DateTimeToSpeech,
    AudioFile,    
}
