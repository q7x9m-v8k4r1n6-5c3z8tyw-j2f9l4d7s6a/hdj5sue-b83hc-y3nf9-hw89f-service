namespace OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardInventory;

// Dùng cho cả Dapper hứng dữ liệu từ DB và MediatR trả về cho API
public sealed class TeamCardInventoryItemModel
{
    public Guid CardId { get; init; }
    public string? CardUrl { get; init; }
    public string CardName { get; init; } = string.Empty;
    public string CardType { get; init; } = string.Empty;
    public string CardStatus { get; init; } = string.Empty;
}