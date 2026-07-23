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
            FROM [dbo].[Teams] t WITH (NOLOCK)
            INNER JOIN [dbo].[Users] u WITH (NOLOCK) ON t.UserId = u.Id
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
            FROM [dbo].[Teams] t WITH (NOLOCK)
            INNER JOIN [dbo].[Users] u WITH (NOLOCK) ON t.UserId = u.Id
            WHERE (u.Role = 'Team' OR u.Role = 'team')
              AND (u.DisplayName LIKE @Keyword OR u.Username LIKE @Keyword)
            ORDER BY u.DisplayName;";
    }

    public static string GetTeamLeaderboardQuery()
    {
        return @"
            SELECT 
                u.DisplayName, 
                t.TotalScore
            FROM [dbo].[Teams] t WITH (NOLOCK)
            INNER JOIN [dbo].[Users] u WITH (NOLOCK) ON t.UserId = u.Id
            WHERE (u.Role = 'Team' OR u.Role = 'team') 
            AND u.Status IN ('1', 'Active', 'active')
            ORDER BY t.TotalScore DESC;"; 
    }
}