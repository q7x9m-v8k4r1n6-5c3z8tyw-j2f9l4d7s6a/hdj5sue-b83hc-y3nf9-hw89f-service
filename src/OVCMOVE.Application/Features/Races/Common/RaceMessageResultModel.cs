namespace OVCMOVE.Application.Features.Races.Common;

public sealed class RaceMessageResultModel
{
    public Guid Id { get; init; }
    public Guid RaceId { get; init; }
    public Guid? SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> RecipientKeys { get; init; } = [];
    public IReadOnlyCollection<string> RecipientLabels { get; init; } = [];
    public string Body { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
