namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class UserQueryHelper
{
    public static string GetByUsernameQuery()
    {
        return @"
            SELECT * 
            FROM [dbo].[Users] WITH (NOLOCK)
            WHERE Username = @Username 
              AND Status = @Status";
    }

    public static string GetByEmailQuery()
    {
        return @"
            SELECT * 
            FROM [dbo].[Users] WITH (NOLOCK)
            WHERE Email = @Email 
              AND Status = @Status";
    }

    public static string GetByEmailAnyStatusQuery()
    {
        return @"
            SELECT * 
            FROM [dbo].[Users] WITH (NOLOCK)
            WHERE Email = @Email";
    }

    public static string GetByIdQuery()
    {
        return @"
            SELECT * 
            FROM [dbo].[Users] WITH (NOLOCK)
            WHERE Id = @Id 
              AND Status = @Status";
    }

    public static string AddUserQuery()
    {
        return @"
            INSERT INTO [dbo].[Users]
                (Id, Username, PasswordHash, Email, Role, DisplayName, Status, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt, IsDeleted)
            VALUES
                (@Id, @Username, @PasswordHash, @Email, @Role, @DisplayName, @Status, @CreatedBy, @CreatedAt, @ModifiedBy, @ModifiedAt, @IsDeleted)";
    }

    public static string UpdateDisplayNameQuery()
    {
        return @"
            UPDATE [dbo].[Users]
            SET DisplayName = @DisplayName,
                ModifiedAt = SYSUTCDATETIME(),
                ModifiedBy = 'google-login'
            WHERE Id = @Id
              AND (DisplayName IS NULL OR LTRIM(RTRIM(DisplayName)) = '')";
    }
}
