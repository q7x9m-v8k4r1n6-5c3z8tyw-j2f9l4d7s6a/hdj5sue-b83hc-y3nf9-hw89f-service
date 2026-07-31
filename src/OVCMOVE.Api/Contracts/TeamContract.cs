namespace OVCMOVE.Api.Contracts;

public static class TeamContract
{
    public sealed class LeaderboardRequest
    {
        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Thiếu RaceId để lấy bảng xếp hạng.")]
        public Guid? RaceId { get; init; }
    }

    public sealed class LeaderboardResponse
    {
        public required TeamScoreSummaryResponse CurrentTeam { get; init; }
        public bool IsLeaderboardVisible { get; init; }
        public bool AreOtherTeamPointsHidden { get; init; }
        public IReadOnlyCollection<LeaderboardEntryResponse> Teams { get; init; } =
            Array.Empty<LeaderboardEntryResponse>();
    }

    public sealed class TeamScoreSummaryResponse
    {
        public Guid TeamId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public int Rank { get; init; }
        public int TotalScore { get; init; }
        public int CompletedRegularBooths { get; init; }
        public int CompletedHiddenBooths { get; init; }
    }

    public sealed class LeaderboardEntryResponse
    {
        public Guid TeamId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public int Rank { get; init; }
        public int? TotalScore { get; init; }
        public bool IsCurrentTeam { get; init; }
    }

    public sealed class ScoreHistoryRequest
    {
        [System.ComponentModel.DataAnnotations.Required(
            ErrorMessage = "Thiếu RaceId để lấy lịch sử điểm.")]
        public Guid? RaceId { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public sealed class ScoreHistoryItemResponse
    {
        public Guid Id { get; init; }
        public Guid? BoothId { get; init; }
        public Guid? OrganizerId { get; init; }
        public int ScoreGiven { get; init; }
        public int ScoreAfterChange { get; init; }
        public string Source { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

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
