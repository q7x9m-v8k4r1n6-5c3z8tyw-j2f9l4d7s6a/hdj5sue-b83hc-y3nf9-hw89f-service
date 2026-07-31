namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class ScoringLogQueries
{
    public static string GetPageQuery() => @"
        SELECT
            [Id],
            [BoothId],
            [OrganizerId],
            [ScoreGiven],
            [ScoreAfterChange],
            [Source],
            [Reason],
            [CreatedAt]
        FROM [dbo].[ScoringLog]
        WHERE [RaceId] = @RaceId
          AND [TeamId] = @TeamId
          AND [IsDeleted] = 0
        ORDER BY [CreatedAt] DESC, [Id] DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public static string CountQuery() => @"
        SELECT COUNT(1)
        FROM [dbo].[ScoringLog]
        WHERE [RaceId] = @RaceId
          AND [TeamId] = @TeamId
          AND [IsDeleted] = 0;";

    public static string GetCompletedBoothStatsQuery() => @"
        SELECT
            COUNT(DISTINCT CASE WHEN b.[IsHidden] = 0 THEN sl.[BoothId] END)
                AS [CompletedRegularBooths],
            COUNT(DISTINCT CASE WHEN b.[IsHidden] = 1 THEN sl.[BoothId] END)
                AS [CompletedHiddenBooths]
        FROM [dbo].[ScoringLog] sl
        INNER JOIN [dbo].[Booth] b
            ON b.[Id] = sl.[BoothId]
           AND b.[RaceId] = sl.[RaceId]
           AND b.[IsDeleted] = 0
        WHERE sl.[RaceId] = @RaceId
          AND sl.[TeamId] = @TeamId
          AND sl.[Source] = N'booth_completed'
          AND sl.[IsDeleted] = 0;";
}
