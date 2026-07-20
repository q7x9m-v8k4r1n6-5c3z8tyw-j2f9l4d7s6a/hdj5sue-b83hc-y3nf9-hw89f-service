namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class OrganizerQueries
{
    public static string GetByEmailQuery()
    {
        return @"
            SELECT
                Id,
                DisplayName,
                Email,
                Role,
                CASE Status
                    WHEN 1 THEN 'active'
                    ELSE 'inactive'
                END AS Status,
                CreatedAt
            FROM [dbo].[Organizers]
            WHERE Email = @Email";
    }

    public static string AddOrganizerQuery()
    {
        return @"
            INSERT INTO [dbo].[Organizers] (Id, DisplayName, Email, Role, Status, CreatedAt)
            VALUES (
                @Id,
                @DisplayName,
                @Email,
                @Role,
                CASE
                    WHEN LOWER(@Status) = 'active' THEN 1
                    ELSE 0
                END,
                @CreatedAt)";
    }

    public static string GetAllOrganizersQuery()
    {
        return @"
            SELECT
                Id,
                DisplayName,
                Email,
                Role,
                CASE Status
                    WHEN 1 THEN 'active'
                    ELSE 'inactive'
                END AS Status
            FROM [dbo].[Organizers]
            ORDER BY DisplayName";
    }

    public static string SearchOrganizerQuery()
    {
        return @"
            SELECT
                Id,
                DisplayName,
                Email,
                Role,
                CASE Status
                    WHEN 1 THEN 'active'
                    ELSE 'inactive'
                END AS Status
            FROM [dbo].[Organizers]
            WHERE DisplayName LIKE @Keyword
               OR Email LIKE @Keyword
            ORDER BY DisplayName";
    }

    public static string GetOrganizerByIdQuery()
    {
        return @"
            SELECT
                Id,
                DisplayName,
                Email,
                Role,
                CASE Status
                    WHEN 1 THEN 'active'
                    ELSE 'inactive'
                END AS Status,
                CreatedAt
            FROM [dbo].[Organizers]
            WHERE Id = @OrganizerId
        ";
    }

    public static string UpdateOrganizerStatusQuery()
    {
        return @"
            UPDATE [dbo].[Organizers]
            SET Status = CASE
                WHEN LOWER(@Status) = 'active' THEN 1
                ELSE 0
            END
            WHERE Id = @OrganizerId
        ";
    }

    public static string UpdateOrganizerUserStatusQuery()
    {
        return @"
            UPDATE [dbo].[Users]
            SET Status = @UserStatus
            WHERE Role = @OrganizerRole
              AND Email = (
                  SELECT Email
                  FROM [dbo].[Organizers]
                  WHERE Id = @OrganizerId
              )
        ";
    }
}
