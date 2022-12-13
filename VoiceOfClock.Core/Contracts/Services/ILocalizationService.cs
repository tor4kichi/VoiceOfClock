using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Core.Contracts.Services;

public interface ILocalizationService
{
    string Translate(string key);
    string Translate(string key, params object[] parameters);
}
