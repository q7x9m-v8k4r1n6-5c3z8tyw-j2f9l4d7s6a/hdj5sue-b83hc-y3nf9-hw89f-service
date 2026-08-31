using OVCMOVE2026.Plugin.Models;

namespace OVCMOVE2026.Plugin.Services;

/// <summary>
/// The plugin owns the built-in card catalog. No card definition is created by
/// an admin and no catalog row is written to SQL Server.
/// </summary>
public static class CardCatalog
{
    public static IReadOnlyCollection<CardDefinition> All { get; } =
    [
        new CardDefinition(
            CardIds.Trap,
            "Trap",
            "Đặt bẫy tại một trạm. Đội đầu tiên yêu cầu vào trạm sẽ bị trừ điểm.",
            0,
            "Chọn một trạm để đặt bẫy. Bẫy được kích hoạt khi có đội request tham gia trạm.",
            [new CardInputDefinition(
                "boothId",
                "Trạm đặt bẫy",
                "booth",
                true,
                "Mã trạm mà đội muốn đặt bẫy.")],
            new Dictionary<string, string> { ["penaltyPoints"] = "10" })
    ];

    public static CardDefinition Get(string cardId) =>
        All.FirstOrDefault(card => string.Equals(card.CardId, cardId, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Không hỗ trợ card '{cardId}'.");
}
