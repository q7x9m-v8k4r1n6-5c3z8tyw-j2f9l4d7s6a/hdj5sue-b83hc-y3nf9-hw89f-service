namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class RefreshTokenQueryHelper
{
    public static string GetByTokenQuery()
    {
        return @"
            SELECT * 
            FROM RefreshTokens 
            WHERE Token = @Token";
    }

    public static string CreateQuery()
    {
        return @"
            INSERT INTO RefreshTokens (UserId, Token, ExpiryDate, IsRevoked)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Token, @ExpiryDate, @IsRevoked)";
    }

    public static string RevokeQuery()
    {
        return @"
            UPDATE RefreshTokens 
            SET IsRevoked = 1 
            WHERE Id = @Id";
    }

    public static string CleanupOldTokensQuery()
    {
        return @"
            DELETE FROM RefreshTokens 
            WHERE ExpiryDate < DATEADD(day, -@DaysToKeep, GETUTCDATE())";
    }
}