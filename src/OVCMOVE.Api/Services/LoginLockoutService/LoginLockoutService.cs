using Microsoft.Extensions.Caching.Memory;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Api.Services.LoginLockoutService;

public class LoginLockoutService(IMemoryCache cache) : ILoginLockoutService
{
    private const int MaxFailedAttemptsBeforeWait = 5;
    private const int MaxFailedAttemptsBeforeBan = 21;
    private const int BaseWaitTimeSeconds = 15;
    private const int WaitTimeMultiplier = 2;

    public void EnsureNotLockedOut(string ipAddress, string username)
    {
        if (cache.TryGetValue($"Ban_IP_{ipAddress}", out string? ipBanReason))
            throw new ApplicationForbiddenException($"[BANNED] IP của bạn đã bị khóa do {ipBanReason}");
            
        if (cache.TryGetValue($"Ban_User_{username}", out string? userBanReason))
            throw new ApplicationForbiddenException($"[BANNED] Tài khoản đã bị khóa do {userBanReason}");

        int waitTimeLeft = 0;

        if (cache.TryGetValue($"Wait_IP_{ipAddress}", out DateTimeOffset ipExpiry)){
            var left = (int)Math.Ceiling((ipExpiry - DateTimeOffset.UtcNow).TotalSeconds);
            if (left > waitTimeLeft) waitTimeLeft = left;
        }

        if (cache.TryGetValue($"Wait_User_{username}", out DateTimeOffset userExpiry)){
            var left = (int)Math.Ceiling((userExpiry - DateTimeOffset.UtcNow).TotalSeconds);
            if (left > waitTimeLeft) waitTimeLeft = left;
        }

        if (waitTimeLeft > 0){
            throw new ApplicationRateLimitException(
                waitTimeLeft, 
                $"Đăng nhập sai quá nhiều. Thử lại sau {waitTimeLeft} giây.");
        }
    }

    public void RecordFailedAttempt(string ipAddress, string username)
    {
        Penalize("IP", ipAddress);
        Penalize("User", username);

        void Penalize(string type, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            IncrementAndPenalize(
                $"Fail_{type}_{value}", 
                $"Wait_{type}_{value}", 
                $"Ban_{type}_{value}");
        }
    }

    public void ResetLockout(string ipAddress, string username)
    {
        cache.Remove($"Fail_IP_{ipAddress}");
        cache.Remove($"Wait_IP_{ipAddress}");
        
        cache.Remove($"Fail_User_{username}");
        cache.Remove($"Wait_User_{username}");
    }

    private void IncrementAndPenalize(string failKey, string waitKey, string banKey)
    {
        var currentFails = cache.GetOrCreate(failKey, entry => {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return 0;
        }) + 1;

        cache.Set(failKey, currentFails, TimeSpan.FromHours(24));

        if (currentFails >= MaxFailedAttemptsBeforeBan){
            cache.Set(
                banKey, 
                "Phát hiện hoạt động đăng nhập bất thường. Vui lòng liên hệ ban tổ chức để được hỗ trợ", 
                TimeSpan.FromHours(24));
            return; 
        }

        if (currentFails % MaxFailedAttemptsBeforeWait == 0)
        {
            int penaltyLevel = currentFails / MaxFailedAttemptsBeforeWait;
            int calculatedWaitTime = BaseWaitTimeSeconds * (int)Math.Pow(WaitTimeMultiplier, penaltyLevel - 1);
            var expiryTime = DateTimeOffset.UtcNow.AddSeconds(calculatedWaitTime);

            cache.Set(waitKey, expiryTime, TimeSpan.FromSeconds(calculatedWaitTime));
        }
    }
}