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