using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceOfClock.Models.Domain
{
    public static class TimersToastNotificationConstants
    {
        public const string ArgumentKey_Action = "action";
        public const string ArgumentValue_Alarm = "alarm";

        public const string ArgumentKey_TimerId = "id";

        public const string ArgumentKey_SnoozeTimeComboBox_Id = "snooze";
        public const string ArgumentKey_SnoozeStop = "stop";
        public const string ArgumentKey_SnoozeAgain = "again";
    }
}
