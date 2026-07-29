namespace OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

public record TeamLeaderboardResultModel
{
    public string DisplayName { get; init; } = string.Empty;
    public int TotalScore { get; init; }
}