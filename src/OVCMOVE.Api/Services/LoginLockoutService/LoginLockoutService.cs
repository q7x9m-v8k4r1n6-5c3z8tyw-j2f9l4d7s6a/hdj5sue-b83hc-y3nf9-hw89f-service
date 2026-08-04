using Microsoft.Extensions.Caching.Memory;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Api.Services.LoginLockoutService;

public class LoginLockoutService(IMemoryCache cache) : ILoginLockoutService
{
    private const int MaxFailedAttemptsBeforeWait = 5;
    private const int MaxFailedAttemptsBeforeBan = 20;

    public void EnsureNotLockedOut(string ipAddress, string username)
    {
        if (cache.TryGetValue($"Ban_IP_{ipAddress}", out string? ipBanReason))
            throw new ApplicationForbiddenException($"[BANNED] IP của bạn đã bị khóa: {ipBanReason}");
            
        if (cache.TryGetValue($"Ban_User_{username}", out string? userBanReason))
            throw new ApplicationForbiddenException($"[BANNED] Tài khoản đã bị khóa: {userBanReason}");

        if (cache.TryGetValue($"Wait_IP_{ipAddress}", out _) || 
            cache.TryGetValue($"Wait_User_{username}", out _))
        {
            throw new ApplicationRateLimitException("Bạn đã thao tác sai quá nhiều. Vui lòng đợi 10 giây.");
        }
    }

    public void RecordFailedAttempt(string ipAddress, string username)
    {
        IncrementAndPenalize($"Fail_IP_{ipAddress}", $"Wait_IP_{ipAddress}", $"Ban_IP_{ipAddress}");
        
        if (!string.IsNullOrWhiteSpace(username))
        {
            IncrementAndPenalize($"Fail_User_{username}", $"Wait_User_{username}", $"Ban_User_{username}");
        }
    }

    public void ResetLockout(string ipAddress, string username)
    {
        cache.Remove($"Fail_IP_{ipAddress}");
        cache.Remove($"Wait_IP_{ipAddress}");
        
        cache.Remove($"Fail_User_{username}");
        cache.Remove($"Wait_User_{username}");
    }

    /// <summary>
    /// Hàm xử lý logic đếm và phạt dùng chung cho cả IP và User
    /// </summary>
    private void IncrementAndPenalize(string failKey, string waitKey, string banKey)
    {
        var currentFails = cache.GetOrCreate(failKey, entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return 0;
        }) + 1;

        cache.Set(failKey, currentFails, TimeSpan.FromHours(24));

        if (currentFails >= MaxFailedAttemptsBeforeBan)
        {
            cache.Set(
                banKey, 
                "Phát hiện hoạt động đăng nhập bất thường. Vui lòng liên hệ ban tổ chức để được hỗ trợ", 
                TimeSpan.FromHours(24));
            return; 
        }

        if (currentFails % MaxFailedAttemptsBeforeWait == 0)
        {
            cache.Set(waitKey, true, TimeSpan.FromSeconds(10));
        }
    }
}