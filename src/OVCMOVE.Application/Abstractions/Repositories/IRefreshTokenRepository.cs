using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CleanupOldTokensAsync(int daysToKeep);
}