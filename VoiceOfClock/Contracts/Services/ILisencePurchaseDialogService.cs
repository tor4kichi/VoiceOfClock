using System.Threading.Tasks;

namespace VoiceOfClock.Contract.Services;

public interface ILisencePurchaseDialogService
{
    Task<bool> ShowPurchaseMainProductConfirmDialogAsync(string dialogTitle);
}
