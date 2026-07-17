namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class TeamQueries
{
    public static string GetAllTeamsQuery()
    {
        return @"
            SELECT Id, Name, LeaderEmail, Username, Status
            FROM Team
            ORDER BY Name;
        ";
    }

    public static string SearchTeamQuery()
    {
        return @"
            SELECT Id, Name, LeaderEmail, Username, Status
            FROM Team
            WHERE Name LIKE @Keyword
               OR Username LIKE @Keyword
            ORDER BY Name;
        ";
    }
}