namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class BoothQueries
{
    /// <summary>
    /// Query lấy thông tin chi tiết của Trạm theo Id
    /// </summary>
    public static string GetBoothByIdQuery()
    {
        return @"
            SELECT  
                Id,
                Name,
                Place,
                Description,
                RaceID,
                TeamId,
                IsHidden,
                [Type],
                [MaximumScore],
                Status,
                CreatedBy,
                CreatedAt,
                ModifiedBy,
                ModifiedAt,
                IsDeleted
            FROM dbo.Booth
            WHERE Id = @Id AND IsDeleted = 0;
        ";
    }

    public static string GetActiveBoothByTeamAndRaceQuery() => @"
        SELECT TOP (1)
            [Id],
            [Name],
            [Place],
            [Description],
            [RaceID],
            [TeamId],
            [IsHidden],
            [Type],
            [MaximumScore],
            [Status],
            [CreatedBy],
            [CreatedAt],
            [ModifiedBy],
            [ModifiedAt],
            [IsDeleted]
        FROM [dbo].[Booth]
        WHERE [RaceID] = @RaceId
          AND [TeamId] = @TeamId
          AND [Status] IN (@PendingStatus, @OccupiedStatus)
          AND [IsDeleted] = 0
        ORDER BY [ModifiedAt] DESC, [Id];";

    public static string TryRequestBoothEntryQuery() => @"
        UPDATE [dbo].[Booth]
        SET
            [Status] = N'pending',
            [TeamId] = @TeamId,
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Id] = @BoothId
          AND [IsDeleted] = 0
          AND [Status] = N'free'
          AND [TeamId] IS NULL;";

    public static string TryOccupyBoothQuery() => @"
        UPDATE [dbo].[Booth]
        SET
            [Status] = N'occupied',
            [TeamId] = @TeamId,
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Id] = @BoothId
          AND [IsDeleted] = 0
          AND [Status] = N'pending'
          AND [TeamId] = @TeamId;";

    public static string TryRejectBoothEntryQuery() => @"
        UPDATE [dbo].[Booth]
        SET
            [Status] = N'free',
            [TeamId] = NULL,
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Id] = @BoothId
          AND [IsDeleted] = 0
          AND [Status] = N'pending'
          AND [TeamId] = @TeamId;";

    public static string TryReleaseBoothQuery() => @"
        UPDATE [dbo].[Booth]
        SET
            [Status] = N'free',
            [TeamId] = NULL,
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Id] = @BoothId
          AND [IsDeleted] = 0
          AND [Status] = N'occupied'
          AND [TeamId] = @TeamId;";

    /// <summary>
    /// Query cộng điểm tích lũy cho Đội chơi
    /// </summary>
    public static string UpdateTeamScoreQuery()
    {
        return @"
            UPDATE rt
            SET rt.TotalScore = rt.TotalScore + @Score
            FROM dbo.RaceTeam rt
            INNER JOIN dbo.Booth b ON b.RaceID = rt.RaceID
            WHERE b.Id = @BoothId
              AND b.Status = N'occupied'
              AND b.TeamId = @TeamId
              AND rt.TeamID = @TeamId
              AND rt.IsDeleted = 0;
        ";
    }

    public static string GetRaceTeamScoreQuery() => @"
        SELECT [TotalScore]
        FROM [dbo].[RaceTeam]
        WHERE [RaceID] = @RaceId
          AND [TeamID] = @TeamId
          AND [IsDeleted] = 0;";

    /// <summary>
    /// Query giải phóng trạng thái Trạm về lại 'Free' sau khi chấm điểm xong
    /// </summary>
    public static string ReleaseBoothStatusQuery()
    {
        return @"
            UPDATE dbo.Booth
            SET Status = 'free', TeamId = NULL
            WHERE Id = @BoothId
              AND Status = N'occupied'
              AND TeamId = @TeamId;
        ";
    }

    /// <summary>
    /// Ghi nhật ký chấm điểm vào bảng ScoringLog mới
    /// </summary>
    public static string InsertScoringLogQuery()
    {
        return @"
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
                @ReasonCode, @Reason, @CreatedBy, GETUTCDATE(), 
                @ModifiedBy, GETUTCDATE(), 0
            );
        ";
    }
}
