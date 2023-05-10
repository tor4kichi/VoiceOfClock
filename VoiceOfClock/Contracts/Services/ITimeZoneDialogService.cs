using System;
using System.Threading.Tasks;

namespace VoiceOfClock.Contracts.Services;
public interface ITimeZoneDialogService
{
    Task<TimeZoneInfo> ChoiceSingleTimeZoneAsync(Predicate<TimeZoneInfo> predicate);
}