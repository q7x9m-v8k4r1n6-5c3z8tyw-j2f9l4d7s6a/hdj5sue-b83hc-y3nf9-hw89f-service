namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class RefreshTokenQueries
{
    private const string RefreshTokenColumns = @"
        [Id], [UserId], [SessionId], [FamilyId], [TokenHash],
        [ReplacedByTokenId], [RevokedAt], [ExpiryDate], [IsRevoked],
        [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]";

    public static string GetByTokenHashQuery()
    {
        return $@"
            SELECT {RefreshTokenColumns}
            FROM [dbo].[RefreshTokens]
            WHERE [TokenHash] = @TokenHash
              AND [IsDeleted] = 0";
    }

    public static string CreateQuery()
    {
        return @"
            INSERT INTO [dbo].[RefreshTokens] (Id, UserId, SessionId, FamilyId, TokenHash, ExpiryDate, IsRevoked, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Id, @UserId, @SessionId, @FamilyId, @TokenHash, @ExpiryDate, @IsRevoked, @CreatedAt)";
    }

    public static string RevokeQuery()
    {
        return @"
            UPDATE [dbo].[RefreshTokens]
            SET IsRevoked = 1,
                RevokedAt = SYSUTCDATETIME()
            WHERE Id = @Id
              AND IsRevoked = 0
              AND IsDeleted = 0";
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
              AND IsDeleted = 0
              AND ExpiryDate > @UtcNow;

            IF @@ROWCOUNT = 1
            BEGIN
                INSERT INTO [dbo].[RefreshTokens]
                    (Id, UserId, SessionId, FamilyId, TokenHash, ExpiryDate, IsRevoked, CreatedAt)
                VALUES
                    (@NewTokenId, @UserId, @SessionId, @FamilyId, @NewTokenHash, @ExpiryDate, 0, @UtcNow);
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
              AND IsRevoked = 0
              AND IsDeleted = 0";
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
