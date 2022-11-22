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
        public const string ArgumentValue_OneShot = "oneshot";

        public const string ArgumentKey_TimerId = "id";

        public const string PropsKey_SnoozeTimeComboBox_Id = "snooze";
        public const string ArgumentKey_SnoozeStop = "stop";
        public const string ArgumentKey_SnoozeAgain = "again";

        public const string ArgumentKey_Confirmed = "confirmed";

        public const int VoiceNotificationRepeatCount = 2;
        public static readonly TimeSpan VoiceNotificationRepeatInterval = TimeSpan.FromSeconds(0.75);

    }
}
