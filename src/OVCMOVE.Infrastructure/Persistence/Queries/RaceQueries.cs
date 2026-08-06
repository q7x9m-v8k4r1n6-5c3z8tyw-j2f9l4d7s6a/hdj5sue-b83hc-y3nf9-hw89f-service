namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class RaceQueries
{
    public static string CreateRaceQuery() => @"
        INSERT INTO [dbo].[Race]
        (
            [Id], [RaceName], [TimeStart], [TimeEnd], [Place], [Status],
            [IsToggledLeaderboard], [IsHiddenPoint], [CoverUrl], [Rules],
            [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        )
        VALUES
        (
            @Id, @RaceName, @TimeStart, @TimeEnd, @Place, @Status,
            @IsToggledLeaderboard, @IsHiddenPoint, @CoverUrl, @Rules,
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
            [Rules] = @Rules,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id
          AND [IsDeleted] = 0
          AND [ModifiedAt] = @ExpectedModifiedAt;";

    public static string GetRaceByIdQuery() => @"
        SELECT
            [Id], [RaceName], [TimeStart], [TimeEnd], [Place],
            [Status],
            [IsToggledLeaderboard], [IsHiddenPoint], [CoverUrl], [Rules],
            [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Race]
        WHERE [Id] = @RaceId AND [IsDeleted] = 0;";

    public static string GetAllRacesQuery() => @"
        SELECT
            R.[Id],
            R.[RaceName] AS [Name],
            R.[RaceName],
            R.[TimeStart],
            R.[TimeEnd],
            R.[Place],
            R.[Status],
            R.[CoverUrl],
            R.[ModifiedAt]
        FROM [dbo].[Race] R
        WHERE R.[IsDeleted] = 0
          AND (
              @TeamId IS NULL
              OR EXISTS (
                  SELECT 1
                  FROM [dbo].[RaceTeam] RT
                  WHERE RT.[RaceID] = R.[Id]
                    AND RT.[TeamID] = @TeamId
                    AND RT.[IsDeleted] = 0
              )
          )
        ORDER BY R.[CreatedAt] DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public static string CountRacesQuery() => @"
        SELECT COUNT(1)
        FROM [dbo].[Race] R
        WHERE R.[IsDeleted] = 0
          AND (
              @TeamId IS NULL
              OR EXISTS (
                  SELECT 1
                  FROM [dbo].[RaceTeam] RT
                  WHERE RT.[RaceID] = R.[Id]
                    AND RT.[TeamID] = @TeamId
                    AND RT.[IsDeleted] = 0
              )
          );";

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
            [Status] = @Status,
            [TeamId] = @TeamId,
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

    public static string GetTeamLeaderboardQuery() => @"
        SELECT
            rt.TeamId,
            CAST(RANK() OVER (ORDER BY rt.TotalScore DESC) AS INT) AS [Rank],
            COALESCE(NULLIF(u.DisplayName, N''), u.Username, u.LinkedEmail) AS DisplayName,
            rt.TotalScore
        FROM [dbo].[RaceTeam] rt
        INNER JOIN [dbo].[Users] u ON rt.TeamId = u.Id
        WHERE rt.RaceId = @RaceId
          AND rt.IsDeleted = 0
          AND u.IsDeleted = 0
        ORDER BY rt.TotalScore DESC, DisplayName, rt.TeamId;";
    public static string GetBoothListQuery() => @"
        SELECT 
            b.Id AS BoothId,
            b.Name AS BoothName,
            b.Place AS BoothLocation,
            b.Description,
            b.Status,
            b.IsHidden,
            tu.DisplayName AS CurrentTeamName,
            ou.DisplayName AS CurrentOrganizerName
        FROM [dbo].[Booth] b
        LEFT JOIN [dbo].[Users] tu ON b.TeamId = tu.Id
        LEFT JOIN [dbo].[BoothOrganizer] bo ON b.Id = bo.BoothId
        LEFT JOIN [dbo].[Users] ou ON bo.OrganizerId = ou.Id
        WHERE b.RaceId = @RaceId
          AND b.IsDeleted = 0
        ORDER BY b.Name ASC;";

    public static string GetScoringLogByRaceIdQuery() => @"
        SELECT
            log.Id AS LogId,
            log.BoothId,
            log.ActorId,
            b.Name AS BoothName,
            log.EventCode,
            log.EventName,
            tu.DisplayName AS TeamName,
            ou.DisplayName AS ActorFullName,
            ou.ShortName AS ActorShortName,
            log.Delta AS ScoreDelta,
            log.ScoreBefore,
            log.ScoreAfter,
            log.ReasonCode,
            log.Reason,
            log.CreatedAt,
            log.CreatedBy
        FROM [dbo].[ScoringLog] log
        LEFT JOIN [dbo].[Booth] b ON log.BoothId = b.Id
        LEFT JOIN [dbo].[Users] tu ON log.TeamId = tu.Id
        LEFT JOIN [dbo].[Users] ou ON log.ActorId = ou.Id
        WHERE log.RaceId = @RaceId
          AND (@TeamId IS NULL OR log.TeamId = @TeamId)
          AND log.IsDeleted = 0
        ORDER BY log.CreatedAt DESC, log.Id DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public static string CountScoringLogByRaceIdQuery() => @"
        SELECT COUNT(1)
        FROM [dbo].[ScoringLog]
        WHERE RaceId = @RaceId
          AND (@TeamId IS NULL OR TeamId = @TeamId)
          AND IsDeleted = 0;";

    public static string GetCompletedBoothStatsQuery() => @"
        SELECT
            COUNT(DISTINCT CASE WHEN b.[IsHidden] = 0 THEN log.[BoothId] END)
                AS [CompletedRegularBooths],
            COUNT(DISTINCT CASE WHEN b.[IsHidden] = 1 THEN log.[BoothId] END)
                AS [CompletedHiddenBooths]
        FROM [dbo].[ScoringLog] log
        INNER JOIN [dbo].[Booth] b
            ON b.[Id] = log.[BoothId]
           AND b.[RaceId] = log.[RaceId]
           AND b.[IsDeleted] = 0
        WHERE log.[RaceId] = @RaceId
          AND log.[TeamId] = @TeamId
          AND log.[ReasonCode] = @CompletedReasonCode
          AND log.[IsDeleted] = 0;";

    public static string GetRaceTeamScoreQuery() => @"
        SELECT [TotalScore]
        FROM [dbo].[RaceTeam]
        WHERE [RaceID] = @RaceId
          AND [TeamID] = @TeamId
          AND [IsDeleted] = 0;";

    public static string UpdateRaceTeamScoreQuery() => @"
        UPDATE [dbo].[RaceTeam]
        SET
            [TotalScore] = @TotalScore,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [RaceID] = @RaceId
          AND [TeamID] = @TeamId
          AND [IsDeleted] = 0;";

    public static string CreateScoringLogQuery() => @"
        INSERT INTO [dbo].[ScoringLog]
        (
            [Id], [EventCode], [EventName], [RaceId], [TeamId],
            [ActorId], [BoothId], [Delta], [ScoreBefore], [ScoreAfter],
            [ReasonCode], [Reason], [CreatedBy], [CreatedAt],
            [ModifiedBy], [ModifiedAt], [IsDeleted]
        )
        VALUES
        (
            @Id, @EventCode, @EventName, @RaceId, @TeamId,
            @ActorId, @BoothId, @Delta, @ScoreBefore, @ScoreAfter,
            @ReasonCode, @Reason, @CreatedBy, @CreatedAt,
            @ModifiedBy, @ModifiedAt, @IsDeleted
        );";

    public static string GetBoothOrganizerByOrganizerAndRaceQuery() => @"
    SELECT TOP 1
        BO.[Id], BO.[BoothId], BO.[OrganizerId],
        BO.[CreatedBy], BO.[CreatedAt], BO.[ModifiedBy], BO.[ModifiedAt], BO.[IsDeleted]
    FROM [dbo].[BoothOrganizer] BO
    INNER JOIN [dbo].[Booth] B
        ON B.[Id] = BO.[BoothId]
       AND B.[IsDeleted] = 0
    WHERE BO.[OrganizerId] = @OrganizerId
      AND B.[RaceID] = @RaceId
      AND BO.[IsDeleted] = 0;";

    public static string CheckBoothOrganizerAssignmentQuery() => @"
        SELECT CASE WHEN EXISTS
        (
            SELECT 1
            FROM [dbo].[BoothOrganizer]
            WHERE [OrganizerId] = @OrganizerId
              AND [BoothId] = @BoothId
              AND [IsDeleted] = 0
        ) THEN 1 ELSE 0 END;";
    public static string CheckTeamInRaceQuery() => @"
    SELECT CASE WHEN EXISTS (
        SELECT 1 FROM dbo.RaceTeam
        WHERE RaceID = @RaceId AND TeamID = @TeamId AND IsDeleted = 0
    ) THEN 1 ELSE 0 END;";
    public static string GetRaceRulesQuery() => @"
    SELECT [Rules]
    FROM [dbo].[Race]
    WHERE [Id] = @RaceId AND [IsDeleted] = 0;";
    public static string GetBoothProgressQuery() => @"
        SELECT
            CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM [dbo].[RaceTeam] rt
                WHERE rt.[RaceID] = @RaceId
                  AND rt.[TeamID] = @TeamId
                  AND rt.[IsDeleted] = 0
            ) THEN 1 ELSE 0 END AS BIT) AS [IsTeamInRace],
            CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM [dbo].[ScoringLog] completed
                WHERE completed.[RaceId] = @RaceId
                  AND completed.[TeamId] = @TeamId
                  AND completed.[BoothId] = @BoothId
                  AND completed.[ReasonCode] = @CompletedReasonCode
                  AND completed.[IsDeleted] = 0
            ) THEN 1 ELSE 0 END AS BIT) AS [HasCompletedBooth],
            COUNT(DISTINCT CASE WHEN booth.[IsHidden] = 0
                THEN log.[BoothId] END) AS [CompletedRegularBooths],
            COUNT(DISTINCT CASE WHEN booth.[IsHidden] = 1
                THEN log.[BoothId] END) AS [CompletedHiddenBooths]
        FROM [dbo].[ScoringLog] log
        INNER JOIN [dbo].[Booth] booth
            ON booth.[Id] = log.[BoothId]
           AND booth.[RaceID] = log.[RaceId]
           AND booth.[IsDeleted] = 0
        WHERE log.[RaceId] = @RaceId
          AND log.[TeamId] = @TeamId
          AND log.[ReasonCode] = @CompletedReasonCode
          AND log.[IsDeleted] = 0;";
}
