namespace OVCMOVE.Application.Features.Booths.Query.GetMyBooth;

public sealed record MyBoothResultModel
{
    public Guid BoothId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Place { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = "other";
    public int? MaximumScore { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? TeamId { get; init; }
    public string? TeamName { get; init; }
}
