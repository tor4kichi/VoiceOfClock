using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Contract.UseCases;

public interface IRunningTimer
{
    string Title { get; }
}
