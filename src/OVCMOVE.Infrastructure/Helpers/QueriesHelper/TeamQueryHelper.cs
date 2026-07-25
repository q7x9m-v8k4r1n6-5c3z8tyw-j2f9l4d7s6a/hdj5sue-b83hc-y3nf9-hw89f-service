namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class TeamQueries
{
    public static string GetAllTeamsQuery()
    {
        return @"
            SELECT Id, DisplayName AS Name, Email AS LeaderEmail, Username, Status
            FROM [dbo].[Users] WITH (NOLOCK)
            WHERE Role = 'team'
            ORDER BY DisplayName;
        ";
    }

    public static string SearchTeamQuery()
    {
        return @"
            SELECT Id, DisplayName AS Name, Email AS LeaderEmail, Username, Status
            FROM [dbo].[Users] WITH (NOLOCK)
            WHERE Role = 'team'
               AND (DisplayName LIKE @Keyword OR Username LIKE @Keyword)
            ORDER BY DisplayName;
        ";
    }
}
