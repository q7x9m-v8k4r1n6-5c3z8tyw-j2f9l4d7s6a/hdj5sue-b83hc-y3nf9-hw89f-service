using Microsoft.Extensions.Logging;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshTokenRepository>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ILogger<RefreshTokenRepository> logger, IDapperHelper dapperHelper) 
        : base(logger, dapperHelper)
    {
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueryHelper.GetByTokenHashQuery();
        var refreshToken =  await _dapperHelper.QueryFirstOrDefaultAsync<RefreshToken>(
            sql, 
            new { TokenHash = tokenHash },
            cancellationToken: cancellationToken);

        return refreshToken;
    }

    public async Task<Guid> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueryHelper.CreateQuery();
            
        var insertedId = await _dapperHelper.ExecuteScalarAsync<Guid>(
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

        return insertedId;
    }

    public async Task<bool> TryRotateAsync(string oldTokenHash, RefreshToken newRefreshToken, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueryHelper.TryRotateQuery();
        return await _dapperHelper.ExecuteScalarAsync<bool>(sql, new
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
        await _dapperHelper.ExecuteAsync(
            RefreshTokenQueryHelper.RevokeFamilyQuery(),
            new { FamilyId = familyId, UtcNow = utcNow },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueryHelper.RevokeQuery();
        
        var rowsAffected = await _dapperHelper.ExecuteAsync(
            sql, 
            new { Id = id }, 
            cancellationToken: cancellationToken);
        
        bool result = rowsAffected > 0;
        return result;
    }

    public async Task<int> CleanupOldTokensAsync(int daysToKeep)
    {
        var sql = RefreshTokenQueryHelper.CleanupOldTokensQuery();

        var numberOfChangedRow = await _dapperHelper.ExecuteAsync(
            sql, 
            new { DaysToKeep = daysToKeep });

        return numberOfChangedRow;
    }
}
