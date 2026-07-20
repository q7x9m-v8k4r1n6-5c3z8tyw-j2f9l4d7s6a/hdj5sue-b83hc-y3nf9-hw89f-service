namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class TeamQueries
{
    public static string GetAllTeamsQuery()
    {
        return @"
            SELECT 
                Id, 
                Name, 
                LeaderEmail, 
                Username, 
                CASE Status
                    WHEN 1 THEN 'active'
                    ELSE 'inactive'
                END AS Status, 
                CreatedAt 
            FROM [dbo].[Teams] 
            ORDER BY Name;";
    }

    public static string SearchTeamQuery()
    {
        return @"
            SELECT 
                Id, 
                Name, 
                LeaderEmail, 
                Username, 
                CASE Status
                    WHEN 1 THEN 'active'
                    ELSE 'inactive'
                END AS Status,
                CreatedAt
            FROM [dbo].[Teams]
            WHERE Name LIKE @Keyword
               OR Username LIKE @Keyword
            ORDER BY Name;";
    }
}