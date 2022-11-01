using DryIoc;
using Microsoft.Toolkit.Uwp.Notifications;
using VoiceOfClock.Models.Domain;
using Windows.Foundation.Collections;

namespace VoiceOfClock.UseCases;

public interface IToastActivationAware
{
    bool ProcessToastActivation(ToastArguments args, ValueSet props);

    static bool IsContainAction(ToastArguments args, string targetAction)
    {       
        return args.TryGetValue(TimersToastNotificationConstants.ArgumentKey_Action, out string action) 
            && action == targetAction
            ;
    }
}
