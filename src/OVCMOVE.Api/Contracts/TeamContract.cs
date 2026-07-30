namespace OVCMOVE.Api.Contracts;

public static class TeamContract
{
    public sealed class CreateTeamRequest
    {
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }

    public sealed class CreateTeamResponse
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = string.Empty;
    }

    public sealed class UpdateTeamRequest
    {
        public string DisplayName { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public bool ResetPassword { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    public sealed class TeamDetailResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string LeaderEmail { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

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
