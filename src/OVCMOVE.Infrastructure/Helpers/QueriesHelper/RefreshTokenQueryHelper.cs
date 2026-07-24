namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class RefreshTokenQueryHelper
{
    public static string GetByTokenHashQuery()
    {
        return @"
            SELECT * 
            FROM [dbo].[RefreshTokens] WITH (NOLOCK)
            WHERE TokenHash = @TokenHash";
    }

    public static string CreateQuery()
    {
        return @"
            INSERT INTO [dbo].[RefreshTokens] (Id, UserId, SessionId, FamilyId, Token, TokenHash, ExpiryDate, IsRevoked, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Id, @UserId, @SessionId, @FamilyId, @TokenHash, @TokenHash, @ExpiryDate, @IsRevoked, @CreatedAt)";
    }

    public static string RevokeQuery()
    {
        return @"
            UPDATE [dbo].[RefreshTokens]
            SET IsRevoked = 1,
                RevokedAt = SYSUTCDATETIME()
            WHERE Id = @Id";
    }

    public static string TryRotateQuery()
    {
        return @"
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            UPDATE [dbo].[RefreshTokens] WITH (UPDLOCK, ROWLOCK)
            SET IsRevoked = 1,
                RevokedAt = @UtcNow,
                ReplacedByTokenId = @NewTokenId
            WHERE TokenHash = @OldTokenHash
              AND IsRevoked = 0
              AND ExpiryDate > @UtcNow;

            IF @@ROWCOUNT = 1
            BEGIN
                INSERT INTO [dbo].[RefreshTokens]
                    (Id, UserId, SessionId, FamilyId, Token, TokenHash, ExpiryDate, IsRevoked, CreatedAt)
                VALUES
                    (@NewTokenId, @UserId, @SessionId, @FamilyId, @NewTokenHash, @NewTokenHash, @ExpiryDate, 0, @UtcNow);
                COMMIT TRANSACTION;
                SELECT CAST(1 AS bit);
            END
            ELSE
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT CAST(0 AS bit);
            END";
    }

    public static string RevokeFamilyQuery()
    {
        return @"
            UPDATE [dbo].[RefreshTokens]
            SET IsRevoked = 1,
                RevokedAt = @UtcNow
            WHERE FamilyId = @FamilyId
              AND IsRevoked = 0";
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
