namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class TeamQueries
{
    public static string GetAllTeamsQuery()
    {
        return @"
            SELECT 
                t.Id, 
                t.UserId,
                t.TotalScore,
                t.CreatedAt,
                u.DisplayName AS Name, 
                u.Email AS LeaderEmail, 
                u.Username, 
                u.Role,
                CASE 
                    WHEN u.Status = '1' OR u.Status = 'Active' OR u.Status = 'active' THEN 'active'
                    ELSE 'inactive'
                END AS Status 
            FROM [dbo].[Teams] t
            INNER JOIN [dbo].[Users] u ON t.UserId = u.Id
            WHERE (u.Role = 'Team' OR u.Role = 'team')
            ORDER BY u.DisplayName;";
    }

    public static string SearchTeamQuery()
    {
        return @"
            SELECT 
                t.Id, 
                t.UserId,
                t.TotalScore,
                t.CreatedAt,
                u.DisplayName AS Name, 
                u.Email AS LeaderEmail, 
                u.Username, 
                u.Role,
                CASE 
                    WHEN u.Status = '1' OR u.Status = 'Active' OR u.Status = 'active' THEN 'active'
                    ELSE 'inactive'
                END AS Status
            FROM [dbo].[Teams] t
            INNER JOIN [dbo].[Users] u ON t.UserId = u.Id
            WHERE (u.Role = 'Team' OR u.Role = 'team')
              AND (u.DisplayName LIKE @Keyword OR u.Username LIKE @Keyword)
            ORDER BY u.DisplayName;";
    }
}
