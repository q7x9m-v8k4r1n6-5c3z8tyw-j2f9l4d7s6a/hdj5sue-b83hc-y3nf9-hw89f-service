namespace OVCMOVE.Application.Features.Teams.Query.GetMySession;

public sealed record MySessionResultModel
{
    public Guid RaceId { get; init; }
    public Guid BoothId { get; init; }
    public string BoothName { get; init; } = string.Empty;
    public string Place { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsHidden { get; init; }
    public string Status { get; init; } = string.Empty;
}
