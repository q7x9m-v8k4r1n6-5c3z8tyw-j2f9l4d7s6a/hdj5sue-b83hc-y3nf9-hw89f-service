namespace OVCMOVE.Application.Abstractions.Services;

public interface ILoginRateLimitService
{
    void CheckIfBanned(string ipAddress, string username);
    void CheckWaitingTime(string ipAddress, string username);
    void RecordFailedAttempt(string ipAddress, string username);
    void ResetLimit(string ipAddress, string username);
    void RemoveBan(string? ipAddress, string? username);
}