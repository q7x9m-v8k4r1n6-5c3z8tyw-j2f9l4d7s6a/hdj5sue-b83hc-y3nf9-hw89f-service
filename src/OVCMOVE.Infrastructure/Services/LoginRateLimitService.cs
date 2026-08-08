using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using OVCMOVE.Application.Abstractions.Services; 
using OVCMOVE.Application.Common;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Infrastructure.Services;

public class LoginRateLimitService : ILoginRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly LoginRateLimitConfigOptions _config;

    public LoginRateLimitService(
        IMemoryCache cache, 
        IOptions<LoginRateLimitConfigOptions> options)
    {
        _cache = cache;
        _config = options.Value;
    }

    public void CheckIfBanned(string ipAddress, string username)
    {
        if (_cache.TryGetValue($"Ban_IP_{ipAddress}", out string? ipBanReason))
            throw new ApplicationForbiddenException($"[BANNED] IP {ipAddress} của bạn đã bị khóa do {ipBanReason}");
            
        if (_cache.TryGetValue($"Ban_User_{username}", out string? userBanReason))
            throw new ApplicationForbiddenException($"[BANNED] Tài khoản đã bị khóa do {userBanReason}");
    }

    public void CheckWaitingTime(string ipAddress, string username)
    {
        int waitTimeLeft = 0;

        if (_cache.TryGetValue($"Wait_IP_{ipAddress}", out DateTimeOffset ipExpiry))
        {
            var left = (int)Math.Ceiling((ipExpiry - DateTimeOffset.UtcNow).TotalSeconds);
            if (left > waitTimeLeft) waitTimeLeft = left;
        }

        if (_cache.TryGetValue($"Wait_User_{username}", out DateTimeOffset userExpiry))
        {
            var left = (int)Math.Ceiling((userExpiry - DateTimeOffset.UtcNow).TotalSeconds);
            if (left > waitTimeLeft) waitTimeLeft = left;
        }

        if (waitTimeLeft > 0)
        {
            throw new ApplicationRateLimitException(
                waitTimeLeft, 
                $"Đăng nhập sai quá nhiều. Thử lại sau {waitTimeLeft} giây.");
        }
    }

    public void RecordFailedAttempt(string ipAddress, string username)
    {
        Penalize(
                $"Fail_IP_{ipAddress}", 
                $"Wait_IP_{ipAddress}", 
                $"Ban_IP_{ipAddress}");
                
        if (string.IsNullOrWhiteSpace(username)) return;
        
        Penalize(
                $"Fail_User_{username}", 
                $"Wait_User_{username}", 
                $"Ban_User_{username}");
    }

    public void ResetLimit(string ipAddress, string username)
    {
        _cache.Remove($"Fail_IP_{ipAddress}");
        _cache.Remove($"Wait_IP_{ipAddress}");
        
        _cache.Remove($"Fail_User_{username}");
        _cache.Remove($"Wait_User_{username}");
    }

    public void RemoveBan(string? ipAddress, string? username)
    {
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            _cache.Remove($"Fail_IP_{ipAddress}");
            _cache.Remove($"Wait_IP_{ipAddress}");
            _cache.Remove($"Ban_IP_{ipAddress}");
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            _cache.Remove($"Fail_User_{username}");
            _cache.Remove($"Wait_User_{username}");
            _cache.Remove($"Ban_User_{username}");
        }
    }

    private void Penalize(string failKey, string waitKey, string banKey)
    {
        var currentFails = _cache.GetOrCreate(failKey, entry => {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return 0;
        }) + 1;

        _cache.Set(failKey, currentFails, TimeSpan.FromHours(24));

        if (currentFails >= _config.MaxFailedAttemptsBeforeBan)
        {
            _cache.Set(
                banKey, 
                "Phát hiện hoạt động đăng nhập bất thường. Vui lòng liên hệ ban tổ chức để được hỗ trợ", 
                TimeSpan.FromHours(24));
            return; 
        }

        if (currentFails % _config.MaxFailedAttemptsBeforeWait == 0)
        {
            int penaltyLevel = currentFails / _config.MaxFailedAttemptsBeforeWait;
            int calculatedWaitTime = _config.BaseWaitTimeSeconds * (int)Math.Pow(_config.WaitTimeMultiplier, penaltyLevel - 1);
            var expiryTime = DateTimeOffset.UtcNow.AddSeconds(calculatedWaitTime);

            _cache.Set(waitKey, expiryTime, TimeSpan.FromSeconds(calculatedWaitTime));
        }
    }
}