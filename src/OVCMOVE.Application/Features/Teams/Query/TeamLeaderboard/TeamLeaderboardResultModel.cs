namespace OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

public sealed record TeamLeaderboardResultModel
{
    public required TeamScoreSummaryResultModel CurrentTeam { get; init; }
    public bool IsLeaderboardVisible { get; init; }
    public bool AreOtherTeamPointsHidden { get; init; }
    public IReadOnlyCollection<TeamLeaderboardEntryResultModel> Teams { get; init; } =
        Array.Empty<TeamLeaderboardEntryResultModel>();
}

public sealed record TeamScoreSummaryResultModel
{
    public Guid TeamId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public int Rank { get; init; }
    public int TotalScore { get; init; }
    public int CompletedRegularBooths { get; init; }
    public int CompletedHiddenBooths { get; init; }
}

public sealed record TeamLeaderboardEntryResultModel
{
    public Guid TeamId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public int Rank { get; init; }
    public int? TotalScore { get; init; }
    public bool IsCurrentTeam { get; init; }
}
