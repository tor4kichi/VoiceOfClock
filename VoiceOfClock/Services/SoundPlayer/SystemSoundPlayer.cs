using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceOfClock.Core.Contracts.Services;
using VoiceOfClock.Core.Models;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace VoiceOfClock.Services.SoundPlayer;

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



