using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Core.Contracts.Models;

public interface IApplicationLifeCycleAware
{
    void Initialize();

    void Suspending();
    void Resuming();
}
