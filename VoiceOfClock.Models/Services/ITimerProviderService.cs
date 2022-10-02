using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Models.Services
{
    public interface ITimerProviderService
    {
        void Suspending();
        void Resuming();
        void Register(string id, DateTime dateTime, Action elapsedAction, Action skipedAction);
        void RefreshTimer(string id);
        void Unregister(string id);

        DateTime GetTargetTime(string id);
    }

    public static class TimerProviderServiceExtensions
    {
        public static void Register(this ITimerProviderService timerProviderService, string id, TimeSpan time, Action elapsedAction, Action skipedAction)
        {
            timerProviderService.Register(id, DateTime.Now + time, elapsedAction, skipedAction);
        }
    }
}
