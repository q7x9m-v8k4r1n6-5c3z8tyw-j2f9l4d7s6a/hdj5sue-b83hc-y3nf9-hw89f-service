using System;

namespace OVCMOVE.Application.DTOs.ResultModels;

public record GetBoothScoringLogsResultModel
{
    public string BoothName { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public string OrganizerName { get; init; } = string.Empty;
    public int ScoreGiven { get; init; }
    public DateTime CreatedAt { get; init; }
}