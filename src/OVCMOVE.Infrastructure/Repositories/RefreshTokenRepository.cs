using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;

namespace OVCMOVE.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshTokenRepository>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ILogger<RefreshTokenRepository> logger, IDapperHelper dapperHelper) 
        : base(logger, dapperHelper)
    {
    }

    public async Task<RefreshTokenEntity?> GetByTokenAsync(string token)
    {
        const string sql = "SELECT * FROM RefreshTokens WHERE Token = @Token";
        return await _dapperHelper.QueryFirstOrDefaultAsync<RefreshTokenEntity>(sql, new { Token = token });
    }

    public async Task<Guid> CreateAsync(RefreshTokenEntity refreshToken)
    {
        const string sql = @"
            INSERT INTO RefreshTokens (UserId, Token, ExpiryDate, IsRevoked)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Token, @ExpiryDate, @IsRevoked)";
            
        var insertedId = await _dapperHelper.ExecuteScalarAsync<Guid>(sql, new 
        { 
            UserId = refreshToken.UserId, 
            Token = refreshToken.Token, 
            ExpiryDate = refreshToken.ExpiryDate, 
            IsRevoked = refreshToken.IsRevoked 
        });

        return insertedId;
    }

    public async Task<bool> RevokeAsync(Guid id)
    {
        const string sql = "UPDATE RefreshTokens SET IsRevoked = 1 WHERE Id = @Id";
        
        var rowsAffected = await _dapperHelper.ExecuteAsync(sql, new { Id = id });
        
        bool result = rowsAffected > 0;
        return result;
    }

    public async Task<int> CleanupOldTokensAsync(int daysToKeep)
    {
        var sql = @"
            DELETE FROM RefreshTokens 
            WHERE ExpiryDate < DATEADD(day, -@DaysToKeep, GETUTCDATE())";

        return await _dapperHelper.ExecuteAsync(sql, new { DaysToKeep = daysToKeep });
    }

}