namespace OVCMOVE.Application.DTOs.ResultModels;

public record GetTeamLeaderboardResultModel
{
    public string DisplayName { get; init; } = string.Empty;
    public int TotalScore { get; init; }
}