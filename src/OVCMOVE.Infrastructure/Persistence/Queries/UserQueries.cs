namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class UserQueries
{
    private const string UserColumns = @"
        [Id], [Username], [PasswordHash], [LinkedEmail], [UserType],
        [DisplayName], [AvatarUrl], [ShortName], [Status],
        [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]";

    public static string GetByUsernameQuery()
    {
        return $@"
            SELECT {UserColumns}
            FROM [dbo].[Users]
            WHERE [Username] = @Username
              AND [Status] = @Status
              AND [IsDeleted] = 0";
    }

    public static string GetByUsernameAnyStatusQuery() => $@"
            SELECT {UserColumns}
            FROM [dbo].[Users]
            WHERE [Username] = @Username
              AND [IsDeleted] = 0";

    public static string GetByEmailQuery()
    {
        return $@"
            SELECT {UserColumns}
            FROM [dbo].[Users]
            WHERE [LinkedEmail] = @LinkedEmail
              AND [Status] = @Status
              AND [IsDeleted] = 0";
    }

    public static string GetByEmailAnyStatusQuery()
    {
        return $@"
            SELECT {UserColumns}
            FROM [dbo].[Users]
            WHERE [LinkedEmail] = @LinkedEmail
              AND [IsDeleted] = 0";
    }

    public static string GetByIdQuery()
    {
        return $@"
            SELECT {UserColumns}
            FROM [dbo].[Users]
            WHERE [Id] = @Id
              AND [Status] = @Status
              AND [IsDeleted] = 0";
    }

    public static string GetByShortNameQuery()
    {
        return $@"
            SELECT {UserColumns}
            FROM [dbo].[Users]
            WHERE [ShortName] = @ShortName
              AND [IsDeleted] = 0";
    }

    public static string AddUserQuery()
    {
        return @"
            INSERT INTO [dbo].[Users]
                (Id, Username, PasswordHash, LinkedEmail, UserType, DisplayName, AvatarUrl, ShortName, Status, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt, IsDeleted)
            VALUES
                (@Id, @Username, @PasswordHash, @LinkedEmail, @UserType, @DisplayName, @AvatarUrl, @ShortName, @Status, @CreatedBy, @CreatedAt, @ModifiedBy, @ModifiedAt, @IsDeleted)";
    }

    public static string UpdateDisplayNameQuery()
    {
        return @"
            UPDATE [dbo].[Users]
            SET DisplayName = @DisplayName,
                ModifiedAt = SYSUTCDATETIME(),
                ModifiedBy = 'google-login'
            WHERE [Id] = @Id
              AND [IsDeleted] = 0
              AND ([DisplayName] IS NULL OR LTRIM(RTRIM([DisplayName])) = '')";
    }

    public static string UpdateGoogleProfileQuery() => @"
        UPDATE [dbo].[Users]
        SET [DisplayName] = COALESCE(NULLIF([DisplayName], N''), @DisplayName),
            [AvatarUrl] = COALESCE(NULLIF([AvatarUrl], N''), @AvatarUrl),
            [ModifiedAt] = SYSUTCDATETIME(),
            [ModifiedBy] = 'google-login'
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string SoftDeleteQuery() => @"
        UPDATE [dbo].[Users]
        SET [IsDeleted] = 1,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id
          AND [UserType] = @UserType
          AND [IsDeleted] = 0;";
}
