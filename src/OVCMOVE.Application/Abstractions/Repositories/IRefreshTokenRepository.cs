using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenEntity?> GetByTokenAsync(string token);
    Task<Guid> CreateAsync(RefreshTokenEntity refreshToken);
    Task<bool> RevokeAsync(Guid id);
    Task<int> CleanupOldTokensAsync(int daysToKeep);
}