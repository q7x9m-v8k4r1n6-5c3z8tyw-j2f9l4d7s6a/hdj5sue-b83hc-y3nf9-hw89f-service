namespace OVCMOVE.Infrastructure.Persistence.Queries;

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
        WHERE [Id] = @Id
          AND [IsDeleted] = 0
          AND [ModifiedAt] = @ExpectedModifiedAt;";

    public static string GetRaceByIdQuery() => @"
        SELECT
            [Id], [RaceName], [TimeStart], [TimeEnd], [Place],
            [Status],
            [IsToggledLeaderboard], [IsHiddenPoint], [CoverUrl],
            [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Race]
        WHERE [Id] = @RaceId AND [IsDeleted] = 0;";

    public static string GetAllRacesQuery() => @"
        SELECT
            [Id],
            [RaceName] AS [Name],
            [RaceName],
            [TimeStart],
            [TimeEnd],
            [Place],
            [Status],
            [CoverUrl],
            [ModifiedAt]
        FROM [dbo].[Race]
        WHERE [IsDeleted] = 0
        ORDER BY [CreatedAt] DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public static string CountRacesQuery() => @"
        SELECT COUNT(1)
        FROM [dbo].[Race]
        WHERE [IsDeleted] = 0;";

    public static string GetRaceDetailQuery() => @"
        SELECT
            [Id],
            [RaceName] AS [Name],
            [RaceName],
            [TimeStart],
            [TimeEnd],
            [Place],
            [Status],
            [CoverUrl],
            [IsToggledLeaderboard],
            [IsHiddenPoint],
            [ModifiedAt]
        FROM [dbo].[Race]
        WHERE [Id] = @RaceId AND [IsDeleted] = 0;";

    public static string GetRaceBoothsQuery() => @"
        SELECT
            [Id], [Name], [Place], [Description]
        FROM [dbo].[Booth] B
        WHERE B.[RaceID] = @RaceId AND B.[IsDeleted] = 0;";

    public static string GetRaceBoothOrganizersQuery() => @"
        SELECT BO.[BoothId], BO.[OrganizerId]
        FROM [dbo].[BoothOrganizer] BO
        INNER JOIN [dbo].[Booth] B
            ON B.[Id] = BO.[BoothId]
           AND B.[IsDeleted] = 0
        WHERE B.[RaceID] = @RaceId
          AND BO.[IsDeleted] = 0
        ORDER BY BO.[BoothId], BO.[OrganizerId];";

    public static string GetBoothsByRaceIdQuery() => @"
        SELECT
            [Id], [Name], [Place], [Description],
            [RaceID] AS [RaceId]
        FROM [dbo].[Booth]
        WHERE [RaceID] = @RaceId AND [IsDeleted] = 0;";

    public static string GetRaceTeamsQuery() => @"
        SELECT
            RT.[TeamID] AS [TeamId],
            COALESCE(NULLIF(U.[DisplayName], N''), U.[Username], U.[LinkedEmail], N'') AS [Name],
            COALESCE(U.[LinkedEmail], N'') AS [LeaderEmail]
        FROM [dbo].[RaceTeam] RT
        LEFT JOIN [dbo].[Users] U ON U.[Id] = RT.[TeamID] AND U.[IsDeleted] = 0
        WHERE RT.[RaceID] = @RaceId AND RT.[IsDeleted] = 0;";

    public static string GetRaceTeamIdsQuery() => @"
        SELECT [TeamID]
        FROM [dbo].[RaceTeam]
        WHERE [RaceID] = @RaceId AND [IsDeleted] = 0;";

    public static string GetRaceOrganizersQuery() => @"
        SELECT [OrganizerID]
        FROM [dbo].[RaceOrganizer]
        WHERE [RaceID] = @RaceId AND [IsDeleted] = 0;";

    public static string GetRaceOrganizerDetailsQuery() => @"
        WITH OrganizerIds AS
        (
            SELECT [OrganizerID] AS [Id]
            FROM [dbo].[RaceOrganizer]
            WHERE [RaceID] = @RaceId AND [IsDeleted] = 0

            UNION

            SELECT BO.[OrganizerId] AS [Id]
            FROM [dbo].[Booth] B
            INNER JOIN [dbo].[BoothOrganizer] BO
                ON BO.[BoothId] = B.[Id]
               AND BO.[IsDeleted] = 0
            WHERE B.[RaceID] = @RaceId
              AND B.[IsDeleted] = 0
        )
        SELECT
            OI.[Id],
            COALESCE(NULLIF(U.[DisplayName], N''), U.[LinkedEmail], N'') AS [DisplayName],
            COALESCE(U.[LinkedEmail], N'') AS [Email],
            U.[AvatarUrl]
        FROM OrganizerIds OI
        LEFT JOIN [dbo].[Users] U ON U.[Id] = OI.[Id] AND U.[IsDeleted] = 0;";

    public static string CreateRaceOrganizerQuery() => @"
        INSERT INTO [dbo].[RaceOrganizer]
            ([Id], [RaceID], [OrganizerID], [CreatedBy], [CreatedAt],
             [ModifiedBy], [ModifiedAt], [IsDeleted])
        VALUES
            (@Id, @RaceId, @OrganizerId, @CreatedBy, @CreatedAt,
             @ModifiedBy, @ModifiedAt, @IsDeleted);";

    public static string CreateRaceTeamQuery() => @"
        INSERT INTO [dbo].[RaceTeam]
            ([Id], [RaceID], [TeamID], [CreatedBy], [CreatedAt],
             [ModifiedBy], [ModifiedAt], [IsDeleted])
        VALUES
            (@Id, @RaceId, @TeamId, @CreatedBy, @CreatedAt,
             @ModifiedBy, @ModifiedAt, @IsDeleted);";

    public static string CreateBoothQuery() => @"
        INSERT INTO [dbo].[Booth]
            ([Id], [Name], [Place], [RaceID], [Description],
             [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt],
             [IsDeleted])
        VALUES
            (@Id, @Name, @Place, @RaceId, @Description,
             @CreatedBy, @CreatedAt, @ModifiedBy,
             @ModifiedAt, @IsDeleted);";

    public static string UpdateBoothQuery() => @"
        UPDATE [dbo].[Booth]
        SET
            [Name] = @Name,
            [Place] = @Place,
            [Description] = @Description,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id AND [RaceID] = @RaceId AND [IsDeleted] = 0;";

    public static string CreateBoothOrganizerQuery() => @"
        INSERT INTO [dbo].[BoothOrganizer]
            ([Id], [BoothId], [OrganizerId], [CreatedBy], [CreatedAt],
             [ModifiedBy], [ModifiedAt], [IsDeleted])
        VALUES
            (@Id, @BoothId, @OrganizerId, @CreatedBy, @CreatedAt,
             @ModifiedBy, @ModifiedAt, @IsDeleted);";

    public static string DeleteBoothOrganizersByBoothIdQuery() => @"
        DELETE FROM [dbo].[BoothOrganizer]
        WHERE [BoothId] = @BoothId;";

    public static string DeleteBoothByIdQuery() => @"
        DELETE FROM [dbo].[Booth]
        WHERE [Id] = @BoothId;";

    public static string DeleteRaceTeamQuery() => @"
        DELETE FROM [dbo].[RaceTeam]
        WHERE [RaceID] = @RaceId AND [TeamID] = @TeamId;";

    public static string DeleteRaceOrganizerQuery() => @"
        DELETE FROM [dbo].[RaceOrganizer]
        WHERE [RaceID] = @RaceId AND [OrganizerID] = @OrganizerId;";
}
