using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Contract.UseCases;

internal interface IApplicationLifeCycleAware
{
    void Initialize();

    void Suspending();
    void Resuming();
}
