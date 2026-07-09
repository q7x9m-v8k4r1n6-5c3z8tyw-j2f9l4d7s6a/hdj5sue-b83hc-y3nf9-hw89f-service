namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class OrganizerQueryHelper
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
            FROM Organizers
            WHERE Email = @Email";
    }

    public static string AddOrganizerQuery()
    {
        return @"
            INSERT INTO Organizers (Id, DisplayName, Email, Role, Status, CreatedAt)
            VALUES (@Id, @DisplayName, @Email, @Role, @Status, @CreatedAt)";
    }
}