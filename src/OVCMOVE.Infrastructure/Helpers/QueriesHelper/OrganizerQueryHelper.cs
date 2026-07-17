namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

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
