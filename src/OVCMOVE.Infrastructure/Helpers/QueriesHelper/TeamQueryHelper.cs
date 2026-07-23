namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class TeamQueries
{
    public static string GetAllTeamsQuery()
    {
        return @"
            SELECT
                t.Id,
                COALESCE(NULLIF(u.DisplayName, ''), u.Username) AS Name,
                u.Email AS LeaderEmail,
                u.Username,
                u.Status
            FROM [dbo].[Teams] t WITH (NOLOCK)
            INNER JOIN [dbo].[Users] u WITH (NOLOCK) ON u.Id = t.UserId
            WHERE u.IsDeleted = 0
            ORDER BY COALESCE(NULLIF(u.DisplayName, ''), u.Username);
        ";
    }

    public static string SearchTeamQuery()
    {
        return @"
            SELECT
                t.Id,
                COALESCE(NULLIF(u.DisplayName, ''), u.Username) AS Name,
                u.Email AS LeaderEmail,
                u.Username,
                u.Status
            FROM [dbo].[Teams] t WITH (NOLOCK)
            INNER JOIN [dbo].[Users] u WITH (NOLOCK) ON u.Id = t.UserId
            WHERE u.IsDeleted = 0
              AND (
                COALESCE(NULLIF(u.DisplayName, ''), u.Username) LIKE @Keyword
                OR u.Username LIKE @Keyword
                OR u.Email LIKE @Keyword
              )
            ORDER BY COALESCE(NULLIF(u.DisplayName, ''), u.Username);
        ";
    }
}
