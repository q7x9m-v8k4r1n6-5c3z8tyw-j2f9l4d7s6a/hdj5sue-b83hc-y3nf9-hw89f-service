namespace OVCMOVE.Application.Features.Teams.Query.ScoreHistory;

public sealed record ScoreHistoryItemResultModel
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
