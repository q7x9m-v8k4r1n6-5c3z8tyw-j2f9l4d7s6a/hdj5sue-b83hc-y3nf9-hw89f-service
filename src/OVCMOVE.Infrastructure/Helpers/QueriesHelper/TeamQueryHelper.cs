namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class TeamQueries
{
    public static string GetByUsernameQuery()
    {
        return @"
            SELECT
                t.Id,
                t.UserId,
                t.TotalScore,
                u.DisplayName AS Name,
                u.Email AS LeaderEmail,
                u.Username,
                u.Status
            FROM [dbo].[Teams] t
            INNER JOIN [dbo].[Users] u ON u.Id = t.UserId
            WHERE u.Username = @Username;
        ";
    }

    public static string GetByLeaderEmailQuery()
    {
        return @"
            SELECT
                t.Id,
                t.UserId,
                t.TotalScore,
                u.DisplayName AS Name,
                u.Email AS LeaderEmail,
                u.Username,
                u.Status
            FROM [dbo].[Teams] t
            INNER JOIN [dbo].[Users] u ON u.Id = t.UserId
            WHERE u.Email = @LeaderEmail;
        ";
    }

    public static string AddTeamQuery()
    {
        return @"
            INSERT INTO [dbo].[Teams] (Id, UserId, TotalScore, CreatedAt)
            VALUES (@Id, @UserId, @TotalScore, @CreatedAt);
        ";
    }

    public static string GetAllTeamsQuery()
    {
        return @"
            SELECT
                t.Id,
                t.UserId,
                t.TotalScore,
                u.DisplayName AS Name,
                u.Email AS LeaderEmail,
                u.Username,
                u.Status
            FROM [dbo].[Teams] t
            INNER JOIN [dbo].[Users] u ON u.Id = t.UserId
            ORDER BY u.DisplayName;
        ";
    }

    public static string SearchTeamQuery()
    {
        return @"
            SELECT
                t.Id,
                t.UserId,
                t.TotalScore,
                u.DisplayName AS Name,
                u.Email AS LeaderEmail,
                u.Username,
                u.Status
            FROM [dbo].[Teams] t
            INNER JOIN [dbo].[Users] u ON u.Id = t.UserId
            WHERE u.DisplayName LIKE @Keyword
               OR u.Username LIKE @Keyword
               OR u.Email LIKE @Keyword
            ORDER BY u.DisplayName;
        ";
    }
}
