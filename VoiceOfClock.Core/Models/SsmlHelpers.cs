﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Core.Models;

// ref@ : https://www.asahi-net.or.jp/~ax2s-kmtn/ref/accessibility/REC-speech-synthesis11-20100907.html#S3.1.5
public static class SsmlHelpers
{
    public static string ToSsml1_0Format(string content, double rate, double pitch, string lang)
    {
        string pitchStr = "default";
        switch (pitch)
        {
            case <= 0.0: pitchStr = "x-low"; break;
            case <= 0.5: pitchStr = "low"; break;
            case <= 1.5: pitchStr = "medium"; break;
            case < 2.0: pitchStr = "high"; break;
            case >= 2.0: pitchStr = "x-high"; break;
        };
       
        return $@"<speak version=""1.0""
xmlns='http://www.w3.org/2001/10/synthesis'
xml:lang=""{lang}""
>
    <prosody rate=""{rate:P0}"" pitch=""{pitchStr}"">
        {content}
    </prosody>
</speak>
";
    }

    public static string ToSsml1_1Format(string content, double rate, double pitch, string lang)
    {
        string pitchPositiveSign = (pitch - 1) > 0.0 ? "+" : "";
        return $@"<speak version=""1.0"" 
xmlns=""http://www.w3.org/2001/10/synthesis""
xml:lang=""{lang}""
>
    <prosody rate=""{rate-1:P0}"" pitch=""{pitchPositiveSign}{pitch-1:P0}"">
        {content}
    </prosody>
</speak>
";
    }

    
}
