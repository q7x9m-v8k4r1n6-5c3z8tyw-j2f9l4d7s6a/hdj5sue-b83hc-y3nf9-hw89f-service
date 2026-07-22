namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class RaceQueries
{
    public static string CreateRaceQuery() => @"
        INSERT INTO [dbo].[Race]
        (
            [Id], [RaceName], [TimeStart], [TimeEnd], [Place], [Status],
            [IsToggledLeaderboard], [IsHiddenPoint], [CoverUrl],
            [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        )
        VALUES
        (
            @Id, @RaceName, @TimeStart, @TimeEnd, @Place, @Status,
            @IsToggledLeaderboard, @IsHiddenPoint, @CoverUrl,
            @CreatedBy, @CreatedAt, @ModifiedBy, @ModifiedAt, @IsDeleted
        );";

    public static string UpdateRaceQuery() => @"
        UPDATE [dbo].[Race]
        SET
            [RaceName] = @RaceName,
            [TimeStart] = @TimeStart,
            [TimeEnd] = @TimeEnd,
            [Place] = @Place,
            [Status] = @Status,
            [IsToggledLeaderboard] = @IsToggledLeaderboard,
            [IsHiddenPoint] = @IsHiddenPoint,
            [CoverUrl] = @CoverUrl,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string GetRaceByIdQuery() => @"
        SELECT
            [Id], [RaceName], [TimeStart], [TimeEnd], [Place], [Status],
            [IsToggledLeaderboard], [IsHiddenPoint], [CoverUrl],
            [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Race] WITH (NOLOCK)
        WHERE [Id] = @RaceId AND [IsDeleted] = 0;";

    public static string GetAllRacesQuery() => @"
        SELECT
            [Id],
            [RaceName] AS [Name],
            [RaceName],
            [TimeStart],
            [TimeEnd],
            [Place],
            CASE
                WHEN [TimeEnd] < SYSUTCDATETIME() THEN 'completed'
                WHEN [TimeStart] <= SYSUTCDATETIME() AND [TimeEnd] >= SYSUTCDATETIME() THEN 'ongoing'
                ELSE 'upcoming'
            END AS [Status],
            [CoverUrl]
        FROM [dbo].[Race] WITH (NOLOCK)
        WHERE [IsDeleted] = 0
        ORDER BY [CreatedAt] DESC;";

    public static string GetRaceDetailQuery() => @"
        SELECT
            [Id],
            [RaceName] AS [Name],
            [RaceName],
            [TimeStart],
            [TimeEnd],
            [Place],
            CASE
                WHEN [TimeEnd] < SYSUTCDATETIME() THEN 'completed'
                WHEN [TimeStart] <= SYSUTCDATETIME() AND [TimeEnd] >= SYSUTCDATETIME() THEN 'ongoing'
                ELSE 'upcoming'
            END AS [Status],
            [CoverUrl],
            [IsToggledLeaderboard],
            [IsHiddenPoint]
        FROM [dbo].[Race] WITH (NOLOCK)
        WHERE [Id] = @RaceId AND [IsDeleted] = 0;";

    public static string GetRaceBoothsQuery() => @"
        SELECT [Name], [Place], [Description], [BoothOrganizerID] AS [OrganizerID]
        FROM [dbo].[Booth] WITH (NOLOCK)
        WHERE [RaceID] = @RaceId;";

    public static string GetRaceTeamsQuery() => @"
        SELECT [TeamID]
        FROM [dbo].[RaceTeam] WITH (NOLOCK)
        WHERE [RaceID] = @RaceId;";

    public static string GetRaceOrganizersQuery() => @"
        SELECT [OrganizerID]
        FROM [dbo].[RaceOrganizer] WITH (NOLOCK)
        WHERE [RaceID] = @RaceId;";

    public static string CreateRaceOrganizerQuery() => @"
        INSERT INTO [dbo].[RaceOrganizer] ([RaceID], [OrganizerID])
        VALUES (@RaceID, @OrganizerID);";

    public static string CreateRaceTeamQuery() => @"
        INSERT INTO [dbo].[RaceTeam] ([RaceID], [TeamID])
        VALUES (@RaceID, @TeamID);";

    public static string CreateBoothQuery() => @"
        INSERT INTO [dbo].[Booth] ([Id], [Name], [Place], [BoothOrganizerID], [RaceID], [Description])
        VALUES (@Id, @Name, @Place, @BoothOrganizerID, @RaceID, @Description);";

    public static string DeleteBoothsByRaceIdQuery() => @"
        DELETE FROM [dbo].[Booth] WHERE [RaceID] = @RaceId;";

    public static string DeleteRaceTeamsByRaceIdQuery() => @"
        DELETE FROM [dbo].[RaceTeam] WHERE [RaceID] = @RaceId;";

    public static string DeleteRaceOrganizersByRaceIdQuery() => @"
        DELETE FROM [dbo].[RaceOrganizer] WHERE [RaceID] = @RaceId;";
}
