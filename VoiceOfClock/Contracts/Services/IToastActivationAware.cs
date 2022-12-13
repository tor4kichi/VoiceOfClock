using DryIoc;
using Microsoft.Toolkit.Uwp.Notifications;
using VoiceOfClock.Core.Models.Timers;
using Windows.Foundation.Collections;

namespace VoiceOfClock.Contracts.Services;

public interface IToastActivationAware
{
    bool ProcessToastActivation(ToastArguments args, ValueSet props);   
}
