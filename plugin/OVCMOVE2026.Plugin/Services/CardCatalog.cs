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
            CardIds.Blackout,
            "Blackout",
            CardTypes.CoreChip,
            "Cướp CD từ một đội ở nhóm đã chọn và phân phối CD cho nhóm đối diện.",
            0,
            "Chọn nhóm cao hơn/thấp hơn và một đội hợp lệ trong nhóm đó.",
            [
                new("targetGroup", "Nhóm mục tiêu", "score_group", true, "higher hoặc lower so với đội sử dụng."),
                new("targetTeamId", "Đội bị cướp", "opponent_team", true, "Đội hợp lệ thuộc nhóm đã chọn.")
            ],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["stealPoints"] = 15,
                ["redistributionPoints"] = 5
            }),
        new(
            CardIds.Taxman,
            "Taxman",
            CardTypes.CoreChip,
            "Đặt thuế tại một booth trong thời gian giới hạn và cướp CD của đội đối thủ đầu tiên đi vào.",
            0,
            "Chọn một booth; chủ Taxman không kích hoạt Taxman của chính mình.",
            [new("boothId", "Booth đặt Taxman", "booth", true, "Booth được đặt Taxman.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 2,
                ["durationMinutes"] = 30,
                ["timeBetweenUseMinutes"] = 20,
                ["stealPoints"] = 20
            }),
        new(
            CardIds.Firewall,
            "Firewall",
            CardTypes.CoreChip,
            "Bảo vệ một booth đã chọn khỏi Trap, Taxman và Blackout; nhận thưởng nếu không bị tấn công.",
            0,
            "Kích hoạt trước khi vào booth được chọn.",
            [new("boothId", "Booth được bảo vệ", "booth", true, "Mỗi lượt Firewall bảo vệ một booth.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 2,
                ["timeBetweenUseMinutes"] = 20,
                ["bonusPoints"] = 25,
                ["blockedCardIds"] = new BsonArray
                {
                    CardIds.Trap,
                    CardIds.Taxman,
                    CardIds.Blackout
                }
            }),
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
            CardIds.Shield,
            "Shield",
            CardTypes.DataPatch,
            "Chặn một hậu quả từ Trap, Taxman, Blackout hoặc Cupid trong thời gian phản hồi.",
            15,
            "Dùng từ thông báo phòng thủ khi một effect hợp lệ đang chờ xử lý.",
            [],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["decisionSeconds"] = 30,
                ["blockedCardIds"] = new BsonArray
                {
                    CardIds.Trap,
                    CardIds.Taxman,
                    CardIds.Blackout,
                    CardIds.Cupid
                }
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
            "Dùng khi đội đang chơi booth; thẻ sẽ được tiêu thụ khi quản trạm xác nhận.",
            [new("boothId", "Booth hiện tại", "booth", true, "Booth đội đang chơi.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["consumeWhen"] = "operator_confirmed"
            }),
        new(
            CardIds.Scout,
            "Scout",
            CardTypes.DataPatch,
            "Nhận ngẫu nhiên một Tech Cache chưa được đội nào nhận.",
            10,
            "Kích hoạt giữa hai booth; thẻ vẫn mất nếu không còn Tech Cache phù hợp.",
            [],
            new BsonDocument
            {
                ["card_use_count_max"] = 1
            }),
        new(
            CardIds.Insight,
            "Insight",
            CardTypes.DataPatch,
            "Xem CD hiện tại của một đội; nhận thêm CD nếu đội sử dụng đang có ít CD hơn.",
            10,
            "Chọn một đội đối thủ giữa hai booth.",
            [new("targetTeamId", "Đội được xem", "opponent_team", true, "Đội cần xem CD.")],
            new BsonDocument
            {
                ["card_use_count_max"] = 1,
                ["bonusPoints"] = 25
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
