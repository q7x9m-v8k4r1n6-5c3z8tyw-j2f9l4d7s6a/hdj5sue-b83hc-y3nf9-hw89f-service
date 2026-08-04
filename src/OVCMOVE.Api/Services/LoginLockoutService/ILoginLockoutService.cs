namespace OVCMOVE.Api.Services.LoginLockoutService;

public interface ILoginLockoutService
{
    void EnsureNotLockedOut(string ipAddress, string username);
    void RecordFailedAttempt(string ipAddress, string username);
    void ResetLockout(string ipAddress, string username);
}