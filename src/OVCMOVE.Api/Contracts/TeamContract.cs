namespace OVCMOVE.Api.Contracts;

public static class TeamContract
{
    public sealed class TeamListItemResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string LeaderEmail { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    public sealed class TeamSearchItemResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string LeaderEmail { get; init; } = string.Empty;
    }
}
