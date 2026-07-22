namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

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
                BoothOrganizerID,
                RaceID,
                IsHidden,
                Status
            FROM dbo.Booth
            WHERE Id = @Id;
        ";
    }

    /// <summary>
    /// Query cộng điểm tích lũy cho Đội chơi
    /// </summary>
    public static string UpdateTeamScoreQuery()
    {
        return @"
            UPDATE dbo.Teams
            SET TotalScore = TotalScore + @Score
            WHERE Id = @TeamId;
        ";
    }

    /// <summary>
    /// Query giải phóng trạng thái Trạm về lại 'Free' sau khi chấm điểm xong
    /// </summary>
    public static string ReleaseBoothStatusQuery()
    {
        return @"
            UPDATE dbo.Booth
            SET Status = 'Free'
            WHERE Id = @BoothId;
        ";
    }

    /// <summary>
    /// Ghi lại danh sách nhập điểm
    /// </summary>
    public static string InsertScoringLogQuery()
    {
        return @"
        INSERT INTO dbo.BoothScoringLogs (Id, BoothId, TeamId, OrganizerId, ScoreGiven, CreatedAt)
        VALUES (@Id, @BoothId, @TeamId, @OrganizerId, @ScoreGiven, GETDATE());
    ";
    }
}
