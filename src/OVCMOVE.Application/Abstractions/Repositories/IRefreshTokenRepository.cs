using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TryRotateAsync(string oldTokenHash, RefreshToken newRefreshToken, DateTime utcNow, CancellationToken cancellationToken = default);
    Task RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> CleanupOldTokensAsync(int daysToKeep);
}
