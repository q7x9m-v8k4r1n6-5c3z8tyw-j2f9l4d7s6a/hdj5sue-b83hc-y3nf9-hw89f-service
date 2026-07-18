namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class RefreshTokenQueryHelper
{
    public static string GetByTokenQuery()
    {
        return @"
            SELECT * 
            FROM [dbo].[RefreshTokens]
            WHERE Token = @Token";
    }

    public static string CreateQuery()
    {
        return @"
            INSERT INTO [dbo].[RefreshTokens] (UserId, Token, ExpiryDate, IsRevoked)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Token, @ExpiryDate, @IsRevoked)";
    }

    public static string RevokeQuery()
    {
        return @"
            UPDATE [dbo].[RefreshTokens]
            SET IsRevoked = 1 
            WHERE Id = @Id";
    }

    public static string CleanupOldTokensQuery()
    {
        return @"
            IF OBJECT_ID(N'[dbo].[RefreshTokens]', N'U') IS NULL
                RETURN;

            DELETE FROM [dbo].[RefreshTokens]
            WHERE ExpiryDate < DATEADD(day, -@DaysToKeep, GETUTCDATE())";
    }
}
