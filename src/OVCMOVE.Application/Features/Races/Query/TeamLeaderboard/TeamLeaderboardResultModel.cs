namespace OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;

public record TeamLeaderboardResultModel
{
    public string DisplayName { get; init; } = string.Empty;
    public int TotalScore { get; init; }
}