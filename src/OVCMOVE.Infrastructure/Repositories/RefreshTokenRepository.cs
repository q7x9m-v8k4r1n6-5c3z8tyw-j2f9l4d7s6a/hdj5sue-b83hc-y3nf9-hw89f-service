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

    public async Task<RefreshTokenEntity?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueryHelper.GetByTokenQuery();
        var refreshToken =  await _dapperHelper.QueryFirstOrDefaultAsync<RefreshTokenEntity>(
            sql, 
            new { Token = token }, 
            cancellationToken: cancellationToken);

        return refreshToken;
    }

    public async Task<Guid> CreateAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default)
    {
        var sql = RefreshTokenQueryHelper.CreateQuery();
            
        var insertedId = await _dapperHelper.ExecuteScalarAsync<Guid>(
            sql,
            new 
            { 
                UserId = refreshToken.UserId, 
                Token = refreshToken.Token, 
                ExpiryDate = refreshToken.ExpiryDate, 
                IsRevoked = refreshToken.IsRevoked 
            }, 
            cancellationToken: cancellationToken);

        return insertedId;
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