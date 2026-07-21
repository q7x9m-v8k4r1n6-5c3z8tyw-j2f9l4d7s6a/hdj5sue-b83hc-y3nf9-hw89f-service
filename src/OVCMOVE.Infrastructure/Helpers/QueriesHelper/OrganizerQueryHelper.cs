namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class OrganizerQueries
{
    public static string GetByEmailQuery()
    {
        return @"
            SELECT
                o.Id,
                o.UserId,
                u.DisplayName,
                u.Email,
                u.Role,
                u.Status,
                o.CreatedAt
            FROM [dbo].[Organizers] o
            INNER JOIN [dbo].[Users] u ON u.Id = o.UserId
            WHERE u.Email = @Email";
    }

    public static string AddOrganizerQuery()
    {
        return @"
            INSERT INTO [dbo].[Organizers] (Id, UserId, CreatedAt)
            VALUES (
                @Id,
                @UserId,
                @CreatedAt)";
    }

    public static string GetAllOrganizersQuery()
    {
        return @"
            SELECT
                o.Id,
                o.UserId,
                u.DisplayName,
                u.Email,
                u.Role,
                u.Status
            FROM [dbo].[Organizers] o
            INNER JOIN [dbo].[Users] u ON u.Id = o.UserId
            ORDER BY u.DisplayName";
    }

    public static string SearchOrganizerQuery()
    {
        return @"
            SELECT
                o.Id,
                o.UserId,
                u.DisplayName,
                u.Email,
                u.Role,
                u.Status
            FROM [dbo].[Organizers] o
            INNER JOIN [dbo].[Users] u ON u.Id = o.UserId
            WHERE u.DisplayName LIKE @Keyword
               OR u.Email LIKE @Keyword
            ORDER BY u.DisplayName";
    }

    public static string GetOrganizerByIdQuery()
    {
        return @"
            SELECT
                o.Id,
                o.UserId,
                u.DisplayName,
                u.Email,
                u.Role,
                u.Status,
                o.CreatedAt
            FROM [dbo].[Organizers] o
            INNER JOIN [dbo].[Users] u ON u.Id = o.UserId
            WHERE o.Id = @OrganizerId
        ";
    }

    public static string UpdateOrganizerUserStatusQuery()
    {
        return @"
            UPDATE [dbo].[Users]
            SET Status = @UserStatus
            WHERE Id = (
                  SELECT UserId
                  FROM [dbo].[Organizers]
                  WHERE Id = @OrganizerId
              )
              AND Role = @OrganizerRole
        ";
    }
}
