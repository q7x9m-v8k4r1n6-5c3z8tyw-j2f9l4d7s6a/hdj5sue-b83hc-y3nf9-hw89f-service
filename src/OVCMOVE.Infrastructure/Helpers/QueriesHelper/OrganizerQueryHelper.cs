namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class OrganizerQueries
{
    public static string GetAllOrganizersQuery()
    {
        return @"
            SELECT Id, DisplayName, Email, Role, Status
            FROM Organizer
            ORDER BY DisplayName;
        ";
    }

    public static string SearchOrganizerQuery()
    {
        return @"
            SELECT Id, DisplayName, Email, Role, Status
            FROM Organizer
            WHERE DisplayName LIKE @Keyword
               OR Email LIKE @Keyword
            ORDER BY DisplayName;
        ";
    }
}

public static class OrganizerQueryHelper
{
    public static string GetOrganizerStatusByIdQuery()
    {
        return """
            SELECT Id, IsActive
            FROM Organizers
            WHERE Id = @OrganizerId
            """;
    }

    public static string UpdateOrganizerStatusQuery()
    {
        return """
            UPDATE Organizers
            SET IsActive = @IsActive
            WHERE Id = @OrganizerId
            """;
    }
}
