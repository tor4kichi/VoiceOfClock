using System.Threading.Tasks;

namespace VoiceOfClock.Contracts.Services;

public interface ILisencePurchaseDialogService
{
    Task<bool> ShowPurchaseMainProductConfirmDialogAsync(string dialogTitle);
}
