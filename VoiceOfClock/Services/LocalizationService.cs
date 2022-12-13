using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;

namespace VoiceOfClock.Services;

internal class LocalizationService : ILocalizationService
{
    public string Translate(string key)
    {
        return key.Translate();
    }

    public string Translate(string key, params object[] parameters)
    {
        return key.Translate(parameters);
    }
}
