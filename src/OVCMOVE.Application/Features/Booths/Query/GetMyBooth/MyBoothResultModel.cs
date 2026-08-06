namespace OVCMOVE.Application.Features.Booths.Query.GetMyBooth;

public sealed record MyBoothResultModel
{
    public Guid BoothId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Place { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
