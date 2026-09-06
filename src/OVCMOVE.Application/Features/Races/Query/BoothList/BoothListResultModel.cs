namespace OVCMOVE.Application.Features.Races.Query.BoothList;

public record BoothListResultModel
{
    public Guid BoothId { get; init; }
    public string BoothName { get; init; } = string.Empty;
    public string BoothLocation {get; init; } = string.Empty;
    public string Description {get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool isHidden { get; init; } = false;
    public string Type { get; init; } = "other";
    public int? MaximumScore { get; init; }
    public string? CurrentTeamName { get; init; }
    public string? CurrentOrganizerName { get; init; }
}
