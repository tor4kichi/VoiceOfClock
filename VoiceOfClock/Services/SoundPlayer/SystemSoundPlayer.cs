using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Contract.Services;
using VoiceOfClock.Models.Domain;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace VoiceOfClock.Services.SoundPlayer;



public enum WindowsNotificationSoundType
{
    Default,
    IM,
    Mail,
    Reminder,
    SMS,
    Looping_Alarm,
    Looping_Alarm2,
    Looping_Alarm3,
    Looping_Alarm4,
    Looping_Alarm5,
    Looping_Alarm6,
    Looping_Alarm7,
    Looping_Alarm8,
    Looping_Alarm9,
    Looping_Alarm10,
    Looping_Call,
    Looping_Call2,
    Looping_Call3,
    Looping_Call4,
    Looping_Call5,
    Looping_Call6,
    Looping_Call7,
    Looping_Call8,
    Looping_Call9,
    Looping_Call10,
}


public sealed class SystemSoundPlayer : ISoundPlayer
{
    public SystemSoundPlayer()
    {
    }

    IEnumerable<SoundSourceToken> ISoundPlayer.GetSoundSources()
    {
        return Enum.GetNames<WindowsNotificationSoundType>()
            .Select(x => new SoundSourceToken(x, SoundSourceType.System, x));            
    }

    SoundSourceType[] ISoundPlayer.SupportedSourceTypes { get; } = new[] { SoundSourceType.System };

    async Task ISoundPlayer.PlayAsync(MediaPlayer mediaPlayer, SoundSourceType soundSourceType, string soundParameter, CancellationToken cancellationToken)
    {
        if (Enum.TryParse(soundParameter, out WindowsNotificationSoundType soundType) is false)
        {
            throw new InvalidOperationException();
        }

        await PlayAsync(mediaPlayer, soundType, cancellationToken);
    }

    public async Task PlayAsync(MediaPlayer mediaPlayer, WindowsNotificationSoundType soundType, CancellationToken cancellationToken = default)
    {
        try
        {            
            using var mediaSource = MediaSource.CreateFromUri(new Uri(soundType.ToMsWinSoundEventUri()));
            await mediaPlayer.PlayAsync(mediaSource, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("SystemSoundPlayer.PlayAsync() was cancelled.");
        }
    }
}





public static class SystemSoundConstants
{
    // see@ https://learn.microsoft.com/en-us/uwp/schemas/tiles/toastschema/element-audio?redirectedfrom=MSDN#attributes
    public const string Default = "ms-winsoundevent:Notification.Default";
    public const string IM = "ms-winsoundevent:Notification.IM";
    public const string Mail = "ms-winsoundevent:Notification.Mail";
    public const string Reminder = "ms-winsoundevent:Notification.Reminder";
    public const string SMS = "ms-winsoundevent:Notification.SMS";
    public const string Looping_Alarm = "ms-winsoundevent:Notification.Looping.Alarm";
    public const string Looping_Alarm2 = "ms-winsoundevent:Notification.Looping.Alarm2";
    public const string Looping_Alarm3 = "ms-winsoundevent:Notification.Looping.Alarm3";
    public const string Looping_Alarm4 = "ms-winsoundevent:Notification.Looping.Alarm4";
    public const string Looping_Alarm5 = "ms-winsoundevent:Notification.Looping.Alarm5";
    public const string Looping_Alarm6 = "ms-winsoundevent:Notification.Looping.Alarm6";
    public const string Looping_Alarm7 = "ms-winsoundevent:Notification.Looping.Alarm7";
    public const string Looping_Alarm8 = "ms-winsoundevent:Notification.Looping.Alarm8";
    public const string Looping_Alarm9 = "ms-winsoundevent:Notification.Looping.Alarm9";
    public const string Looping_Alarm10 = "ms-winsoundevent:Notification.Looping.Alarm10";
    public const string Looping_Call = "ms-winsoundevent:Notification.Looping.Call";
    public const string Looping_Call2 = "ms-winsoundevent:Notification.Looping.Call2";
    public const string Looping_Call3 = "ms-winsoundevent:Notification.Looping.Call3";
    public const string Looping_Call4 = "ms-winsoundevent:Notification.Looping.Call4";
    public const string Looping_Call5 = "ms-winsoundevent:Notification.Looping.Call5";
    public const string Looping_Call6 = "ms-winsoundevent:Notification.Looping.Call6";
    public const string Looping_Call7 = "ms-winsoundevent:Notification.Looping.Call7";
    public const string Looping_Call8 = "ms-winsoundevent:Notification.Looping.Call8";
    public const string Looping_Call9 = "ms-winsoundevent:Notification.Looping.Call9";
    public const string Looping_Call10 = "ms-winsoundevent:Notification.Looping.Call10";



    public static string ToMsWinSoundEventUri(this WindowsNotificationSoundType type)
    {
        return type switch
        {
            WindowsNotificationSoundType.Default => Default,
            WindowsNotificationSoundType.IM => IM,
            WindowsNotificationSoundType.Mail => Mail,
            WindowsNotificationSoundType.Reminder => Reminder,
            WindowsNotificationSoundType.SMS => SMS,
            WindowsNotificationSoundType.Looping_Alarm => Looping_Alarm,
            WindowsNotificationSoundType.Looping_Alarm2 => Looping_Alarm2,
            WindowsNotificationSoundType.Looping_Alarm3 => Looping_Alarm3,
            WindowsNotificationSoundType.Looping_Alarm4 => Looping_Alarm4,
            WindowsNotificationSoundType.Looping_Alarm5 => Looping_Alarm5,
            WindowsNotificationSoundType.Looping_Alarm6 => Looping_Alarm6,
            WindowsNotificationSoundType.Looping_Alarm7 => Looping_Alarm7,
            WindowsNotificationSoundType.Looping_Alarm8 => Looping_Alarm8,
            WindowsNotificationSoundType.Looping_Alarm9 => Looping_Alarm9,
            WindowsNotificationSoundType.Looping_Alarm10 => Looping_Alarm10,
            WindowsNotificationSoundType.Looping_Call => Looping_Call,
            WindowsNotificationSoundType.Looping_Call2 => Looping_Call2,
            WindowsNotificationSoundType.Looping_Call3 => Looping_Call3,
            WindowsNotificationSoundType.Looping_Call4 => Looping_Call4,
            WindowsNotificationSoundType.Looping_Call5 => Looping_Call5,
            WindowsNotificationSoundType.Looping_Call6 => Looping_Call6,
            WindowsNotificationSoundType.Looping_Call7 => Looping_Call7,
            WindowsNotificationSoundType.Looping_Call8 => Looping_Call8,
            WindowsNotificationSoundType.Looping_Call9 => Looping_Call9,
            WindowsNotificationSoundType.Looping_Call10 => Looping_Call10,
            _ => throw new NotSupportedException(type.ToString()),
        };
    }

    public static string[] GetAllNotificationSoundUris()
    {
        return new[]
        {
            Default,
            IM,
            Mail,
            Reminder,
            SMS,
            Looping_Alarm,
            Looping_Alarm2,
            Looping_Alarm3,
            Looping_Alarm4,
            Looping_Alarm5,
            Looping_Alarm6,
            Looping_Alarm7,
            Looping_Alarm8,
            Looping_Alarm9,
            Looping_Alarm10,
            Looping_Call,
            Looping_Call2,
            Looping_Call3,
            Looping_Call4,
            Looping_Call5,
            Looping_Call6,
            Looping_Call7,
            Looping_Call8,
            Looping_Call9,
            Looping_Call10,
        };
    }

    public static string[] GetNotificationSoundUris()
    {
        return new[]
        {
            Default,
            IM,
            Mail,
            Reminder,
            SMS,

        };
    }

    public static string[] GetNotificationLoopingSoundUris()
    {
        return new[]
        {
            Looping_Alarm,
            Looping_Alarm2,
            Looping_Alarm3,
            Looping_Alarm4,
            Looping_Alarm5,
            Looping_Alarm6,
            Looping_Alarm7,
            Looping_Alarm8,
            Looping_Alarm9,
            Looping_Alarm10,
            Looping_Call,
            Looping_Call2,
            Looping_Call3,
            Looping_Call4,
            Looping_Call5,
            Looping_Call6,
            Looping_Call7,
            Looping_Call8,
            Looping_Call9,
            Looping_Call10,
        };
    }
}



