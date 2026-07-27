using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbExecutor _db;

    public RefreshTokenRepository(IDbExecutor db) =>
        _db = db;

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueries.GetByTokenHashQuery();
        var refreshToken = await _db.QueryFirstOrDefaultAsync<RefreshToken>(
            sql,
            new { TokenHash = tokenHash },
            cancellationToken: cancellationToken);

        return refreshToken;
    }

    public async Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueries.CreateQuery();

        var insertedId = await _db.ExecuteScalarAsync<Guid>(
            sql,
            new
            {
                Id = refreshToken.Id,
                UserId = refreshToken.UserId,
                SessionId = refreshToken.SessionId,
                FamilyId = refreshToken.FamilyId,
                TokenHash = refreshToken.TokenHash,
                ExpiryDate = refreshToken.ExpiryDate,
                IsRevoked = refreshToken.IsRevoked,
                CreatedAt = refreshToken.CreatedAt
            },
            cancellationToken: cancellationToken);

        if (insertedId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Refresh token insert did not return an identifier.");
        }
    }

    public async Task<bool> TryRotateAsync(string oldTokenHash, RefreshToken newRefreshToken, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueries.TryRotateQuery();
        return await _db.ExecuteScalarAsync<bool>(sql, new
        {
            OldTokenHash = oldTokenHash,
            NewTokenId = newRefreshToken.Id,
            newRefreshToken.UserId,
            newRefreshToken.SessionId,
            newRefreshToken.FamilyId,
            NewTokenHash = newRefreshToken.TokenHash,
            newRefreshToken.ExpiryDate,
            UtcNow = utcNow
        }, cancellationToken: cancellationToken);
    }

    public async Task RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync(
            RefreshTokenQueries.RevokeFamilyQuery(),
            new { FamilyId = familyId, UtcNow = utcNow },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueries.RevokeQuery();

        var rowsAffected = await _db.ExecuteAsync(
            sql,
            new { Id = id },
            cancellationToken: cancellationToken);

        bool result = rowsAffected > 0;
        return result;
    }

    public async Task<int> CleanupOldTokensAsync(
        int daysToKeep,
        CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueries.CleanupOldTokensQuery();

        var numberOfChangedRow = await _db.ExecuteAsync(
            sql,
            new { DaysToKeep = daysToKeep },
            cancellationToken: cancellationToken);

        return numberOfChangedRow;
    }
}
