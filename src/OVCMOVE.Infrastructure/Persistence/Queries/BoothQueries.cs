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
            WHERE b.Id = @BoothId AND rt.TeamID = @TeamId;
        ";
    }

    /// <summary>
    /// Query giải phóng trạng thái Trạm về lại 'Free' sau khi chấm điểm xong
    /// </summary>
    public static string ReleaseBoothStatusQuery()
    {
        return @"
            UPDATE dbo.Booth
            SET Status = 'free'
            WHERE Id = @BoothId;
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