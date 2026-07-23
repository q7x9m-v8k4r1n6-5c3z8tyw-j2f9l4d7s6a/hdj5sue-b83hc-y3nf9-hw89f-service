namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class BoothScoringLogQueries
{
    public static string GetScoringLogsQuery(int? limit)
    {
        // Kỹ thuật chèn TOP động: Nếu có limit thì thêm "TOP (X)", nếu null thì rỗng (lấy hết)
        string topClause = limit.HasValue ? $"TOP ({limit.Value})" : "";

        return $@"
            SELECT {topClause}
                b.Name AS BoothName,
                tu.DisplayName AS TeamName,
                ou.DisplayName AS OrganizerName,
                l.ScoreGiven,
                l.CreatedAt
            FROM [dbo].[BoothScoringLogs] l WITH (NOLOCK)
            INNER JOIN [dbo].[Booth] b WITH (NOLOCK) ON l.BoothId = b.Id
            
            -- Lấy tên Đội (Đi qua bảng Teams rồi chọc vào Users)
            INNER JOIN [dbo].[Teams] t WITH (NOLOCK) ON l.TeamId = t.Id
            INNER JOIN [dbo].[Users] tu WITH (NOLOCK) ON t.UserId = tu.Id
            
            -- Lấy tên Ban Tổ Chức (Đi qua bảng Organizers rồi chọc vào Users)
            INNER JOIN [dbo].[Organizers] o WITH (NOLOCK) ON l.OrganizerId = o.Id
            INNER JOIN [dbo].[Users] ou WITH (NOLOCK) ON o.UserId = ou.Id
            
            -- Mặc định xếp lịch sử mới nhất lên đầu
            ORDER BY l.CreatedAt DESC;";
    }
}