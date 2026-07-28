namespace OVCMOVE.Api.Contracts;

public static class TeamContract
{
    /// <summary>
    /// Request phân trang cho API lấy danh sách Teams
    /// </summary>
    public sealed class GetTeamsRequest
    {
        public int PageIndex { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    /// <summary>
    /// Response trả về danh sách Teams
    /// </summary>
    public sealed class TeamListItemResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string LeaderEmail { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response trả về khi tìm kiếm Team (Search)
    /// </summary>
    public sealed class TeamSearchItemResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string LeaderEmail { get; init; } = string.Empty;
    }
}