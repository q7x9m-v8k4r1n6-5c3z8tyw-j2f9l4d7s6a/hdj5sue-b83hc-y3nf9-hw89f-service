using MongoDB.Bson;
using OVCMOVE.Application.Common;
using OVCMOVE2026.Plugin.Models;

namespace OVCMOVE2026.Plugin.Services;

/// <summary>
/// Code selects the typed gameplay handler by cardId. Mongo cardConfig only
/// stores the gameplay numbers that admins may tune for one race.
/// </summary>
public static class CardCatalog
{
    public static IReadOnlyCollection<CardDefinition> All { get; } =
    [
        new(
            CardIds.Overclock,
            "Overclock",
            CardTypes.CoreChip,
            "Dự đoán một booth thất bại của từng đội đối thủ sau giai đoạn chơi booth.",
            0,
            "Gửi toàn bộ dự đoán một lần khi admin mở Overclock.",
            [new("predictions", "Danh sách dự đoán", "overclock_predictions", true,
                "Mỗi phần tử gồm targetTeamId và boothId.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["cdSteal"] = 15,
                ["cdSelfPenalty"] = 5
            }),
        new(
            CardIds.Cupid,
            "Cupid",
            CardTypes.CoreChip,
            "Theo dõi kết quả finalized tiếp theo của một đội đối thủ.",
            0,
            "Chọn một đội khi chưa có lượt Cupid nào của card này đang chờ.",
            [new("targetTeamId", "Đội được chọn", "opponent_team", true, "Đội đối thủ được theo dõi.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 3,
                ["timeBetweenUseMinutes"] = 15,
                ["rewardMultiplier"] = 1.0,
                ["failurePenalty"] = 5
            }),
        new(
            CardIds.Engineer,
            "Engineer",
            CardTypes.DataPatch,
            "Nhân đôi điểm GSV trao ở booth trí óc phù hợp tiếp theo.",
            15,
            "Kích hoạt trước khi bắt đầu booth trí óc.",
            [],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["requiredBoothType"] = "intellectual",
                ["scoreMultiplier"] = 2.0
            }),
        new(
            CardIds.Athlete,
            "Athlete",
            CardTypes.DataPatch,
            "Nhân đôi điểm khi đạt điểm tối đa ở booth thể chất phù hợp tiếp theo.",
            15,
            "Kích hoạt trước khi bắt đầu booth thể chất.",
            [],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["requiredBoothType"] = "physical",
                ["scoreMultiplier"] = 2.0,
                ["qualificationMode"] = "score_equals_booth_max"
            }),
        new(
            CardIds.Revive,
            "Revive",
            CardTypes.DataPatch,
            "Yêu cầu quản trạm cho chơi lại trước khi booth được kết thúc.",
            15,
            "Dùng khi đội đang chơi booth; card chỉ bị trừ sau khi quản trạm xác nhận.",
            [new("boothId", "Booth hiện tại", "booth", true, "Booth đội đang chơi.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["consumeWhen"] = "operator_confirmed"
            }),
        new(
            CardIds.Swap,
            "Swap",
            CardTypes.DataPatch,
            "Thông báo hai đội và BTC/GSV để xử lý mảnh bản đồ thủ công.",
            10,
            "Chọn một đội đối thủ giữa hai booth.",
            [new("targetTeamId", "Đội được chọn", "opponent_team", true, "Đội cần liên hệ.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["mapPieceViewLimit"] = 4
            }),
        new(
            CardIds.Trap,
            "Trap",
            CardTypes.DataPatch,
            "Đặt bẫy tại một booth. Đội đối thủ đầu tiên request booth sẽ kích hoạt bẫy.",
            15,
            "Chọn một booth để đặt bẫy.",
            [new("boothId", "Booth đặt bẫy", "booth", true, "Booth được đặt bẫy.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["penaltyPoints"] = 15
            })
    ];

    public static CardDefinition Get(string cardId) =>
        TryGet(cardId)
        ?? throw new ApplicationNotFoundException($"Không hỗ trợ card '{cardId}'.");

    public static CardDefinition? TryGet(string cardId) =>
        All.FirstOrDefault(card => string.Equals(card.CardId, cardId, StringComparison.OrdinalIgnoreCase));
}
