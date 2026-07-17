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
                Status,
                CreatedAt
            FROM [dbo].[Organizers]
            WHERE Email = @Email";
    }

    public static string AddOrganizerQuery()
    {
        return @"
            INSERT INTO [dbo].[Organizers] (Id, DisplayName, Email, Role, Status, CreatedAt)
            VALUES (@Id, @DisplayName, @Email, @Role, @Status, @CreatedAt)";
    }

    public static string GetAllOrganizersQuery()
    {
        return @"
            SELECT
                Id,
                DisplayName,
                Email,
                Role,
                Status
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
                Status
            FROM [dbo].[Organizers]
            WHERE DisplayName LIKE @Keyword
               OR Email LIKE @Keyword
            ORDER BY DisplayName";
    }
}
