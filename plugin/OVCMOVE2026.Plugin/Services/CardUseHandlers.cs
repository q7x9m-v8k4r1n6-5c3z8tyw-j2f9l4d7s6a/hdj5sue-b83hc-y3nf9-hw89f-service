using MongoDB.Bson;
using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE.Domain.Constants;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public sealed record CardUseContext(
    Guid RaceId,
    Guid TeamId,
    CardInventoryState Inventory,
    TeamCardState TeamCard,
    string CardUseId,
    BsonDocument Inputs,
    DateTime OccurredAt);

public sealed record CardUseNotification(string RecipientType, Guid? RecipientId, string Message);

public sealed record CardUsePlan(
    string Status,
    bool ConsumeNow,
    string Message,
    CardEffectDocument? Effect = null,
    BsonDocument? Result = null,
    IReadOnlyCollection<CardUseNotification>? Notifications = null);

public interface ICardUseHandler
{
    string CardId { get; }
    Task<CardUsePlan> PrepareAsync(CardUseContext context, CancellationToken cancellationToken);
}

public sealed class CardUseHandlerResolver(IEnumerable<ICardUseHandler> handlers)
{
    private readonly IReadOnlyDictionary<string, ICardUseHandler> _handlers = handlers
        .ToDictionary(handler => handler.CardId, StringComparer.OrdinalIgnoreCase);

    public ICardUseHandler Resolve(string cardId) =>
        _handlers.TryGetValue(cardId, out var handler)
            ? handler
            : throw new ApplicationValidationException($"Card '{cardId}' chưa có gameplay handler.");
}

public abstract class EffectCardUseHandler : ICardUseHandler
{
    public abstract string CardId { get; }
    public abstract Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken);

    protected static CardEffectDocument CreateEffect(
        CardUseContext context,
        string triggerEventCode,
        string? targetTeamId = null,
        string? targetBoothId = null,
        BsonDocument? data = null,
        DateTime? limitEndAt = null) => new()
        {
            RaceId = context.RaceId.ToString(),
            CardId = context.TeamCard.CardInfo.CardId,
            CardInstanceId = context.TeamCard.CardInfo.CardInstanceId,
            CardUseId = context.CardUseId,
            OwnerTeamId = context.TeamId.ToString(),
            TargetTeamId = targetTeamId,
            TargetBoothId = targetBoothId,
            TriggerEventCode = triggerEventCode,
            Status = CardEffectStatus.Active,
            StartAt = context.OccurredAt,
            LimitEndAt = limitEndAt,
            CreatedAt = context.OccurredAt,
            CreatedBy = context.TeamId.ToString(),
            ModifiedAt = context.OccurredAt,
            ModifiedBy = context.TeamId.ToString(),
            Data = data ?? new BsonDocument()
        };

    protected static Guid RequireGuid(BsonDocument inputs, string key, string message)
    {
        if (!inputs.TryGetValue(key, out var value) || !value.IsString ||
            !Guid.TryParse(value.AsString, out var id) || id == Guid.Empty)
            throw new ApplicationValidationException(message);
        return id;
    }

    protected static Task<CardUsePlan> Result(CardUsePlan plan) => Task.FromResult(plan);
}

public sealed class BlackoutCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Blackout;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        var targetTeamId = RequireGuid(
            context.Inputs,
            "targetTeamId",
            "Blackout cần targetTeamId hợp lệ.");
        if (targetTeamId == context.TeamId)
            throw new ApplicationValidationException("Blackout chỉ được chọn đội đối thủ.");
        var targetGroup = context.Inputs.GetString("targetGroup").Trim().ToLowerInvariant();
        if (targetGroup is not ("higher" or "lower"))
            throw new ApplicationValidationException("targetGroup của Blackout phải là higher hoặc lower.");

        var effect = CreateEffect(
            context,
            CardEffectEventCodes.DefenseDecision,
            targetTeamId: targetTeamId.ToString(),
            data: new BsonDocument
            {
                ["targetGroup"] = targetGroup,
                ["stealPoints"] = context.Inventory.CardConfig.GetInt("stealPoints", 15),
                ["redistributionPoints"] = context.Inventory.CardConfig.GetInt("redistributionPoints", 5)
            });
        return Result(new CardUsePlan(
            CardUseStatus.Active,
            true,
            "Blackout đang chờ xử lý phòng thủ của đội mục tiêu.",
            effect));
    }
}

public sealed class TaxmanCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Taxman;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        if (context.TeamCard.CardUses.Any(use => use.Status == CardUseStatus.Active))
            throw new ApplicationConflictException("Lượt Taxman trước vẫn còn hiệu lực.");
        var boothId = RequireGuid(context.Inputs, "boothId", "Taxman cần boothId hợp lệ.");
        var durationMinutes = context.Inventory.CardConfig.GetInt("durationMinutes", 30);
        var effect = CreateEffect(
            context,
            CardEffectEventCodes.BoothEntryRequested,
            targetBoothId: boothId.ToString(),
            data: new BsonDocument
            {
                ["stealPoints"] = context.Inventory.CardConfig.GetInt("stealPoints", 20),
                ["timeBetweenUseMinutes"] = context.Inventory.CardConfig.GetInt("timeBetweenUseMinutes", 20)
            },
            limitEndAt: context.OccurredAt.AddMinutes(durationMinutes));
        return Result(new CardUsePlan(
            CardUseStatus.Active,
            true,
            $"Đã đặt Taxman trong {durationMinutes} phút.",
            effect));
    }
}

public sealed class FirewallCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Firewall;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        if (context.TeamCard.CardUses.Any(use => use.Status == CardUseStatus.Active))
            throw new ApplicationConflictException("Lượt Firewall trước vẫn đang bảo vệ một booth.");
        var boothId = RequireGuid(context.Inputs, "boothId", "Firewall cần boothId hợp lệ.");
        var effect = CreateEffect(
            context,
            CardEffectEventCodes.BoothResultFinalized,
            targetTeamId: context.TeamId.ToString(),
            targetBoothId: boothId.ToString(),
            data: new BsonDocument
            {
                ["bonusPoints"] = context.Inventory.CardConfig.GetInt("bonusPoints", 25),
                ["timeBetweenUseMinutes"] = context.Inventory.CardConfig.GetInt("timeBetweenUseMinutes", 20),
                ["blockedCardIds"] = context.Inventory.CardConfig.TryGetValue("blockedCardIds", out var blocked)
                    ? blocked.DeepClone()
                    : new BsonArray { CardIds.Trap, CardIds.Taxman, CardIds.Blackout },
                ["wasAttacked"] = false
            });
        return Result(new CardUsePlan(
            CardUseStatus.Active,
            true,
            "Firewall đang bảo vệ booth đã chọn.",
            effect));
    }
}

public sealed class ShieldCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Shield;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken) =>
        throw new ApplicationConflictException(
            "Shield chỉ được sử dụng từ thông báo phòng thủ của một effect đang chờ.");
}

public sealed class OverclockCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Overclock;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Inputs.TryGetValue("predictions", out var raw) || !raw.IsBsonArray ||
            raw.AsBsonArray.Count == 0 || raw.AsBsonArray.Any(item => !item.IsBsonDocument))
            throw new ApplicationValidationException("Overclock cần danh sách predictions hợp lệ.");

        var targetTeamIds = new HashSet<Guid>();
        foreach (var prediction in raw.AsBsonArray.Select(item => item.AsBsonDocument))
        {
            var targetTeamId = RequireGuid(
                prediction, "targetTeamId", "Mỗi dự đoán cần targetTeamId hợp lệ.");
            _ = RequireGuid(prediction, "boothId", "Mỗi dự đoán cần boothId hợp lệ.");
            if (targetTeamId == context.TeamId)
                throw new ApplicationValidationException("Overclock không được dự đoán đội của chính mình.");
            if (!targetTeamIds.Add(targetTeamId))
                throw new ApplicationValidationException("Mỗi đội đối thủ chỉ được dự đoán một lần.");
        }

        var effect = CreateEffect(
            context,
            CardEffectEventCodes.OverclockResolution,
            data: new BsonDocument
            {
                ["predictions"] = raw.DeepClone(),
                ["cdSteal"] = context.Inventory.CardConfig.GetInt("cdSteal", 15),
                ["cdSelfPenalty"] = context.Inventory.CardConfig.GetInt("cdSelfPenalty", 5)
            });
        return Result(new CardUsePlan(
            CardUseStatus.Active,
            true,
            "Đã khóa danh sách dự đoán Overclock.",
            effect));
    }
}

public sealed class CupidCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Cupid;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        var targetTeamId = RequireGuid(context.Inputs, "targetTeamId", "Cupid cần targetTeamId hợp lệ.");
        if (targetTeamId == context.TeamId)
            throw new ApplicationValidationException("Cupid chỉ được chọn đội đối thủ.");
        if (context.TeamCard.CardUses.Any(use => use.Status == CardUseStatus.Active))
            throw new ApplicationConflictException("Lượt Cupid trước vẫn đang chờ kết quả finalized.");

        var effect = CreateEffect(
            context,
            CardEffectEventCodes.BoothResultFinalized,
            targetTeamId: targetTeamId.ToString(),
            data: new BsonDocument
            {
                ["rewardMultiplier"] = context.Inventory.CardConfig.GetDouble("rewardMultiplier", 1),
                ["failurePenalty"] = context.Inventory.CardConfig.GetInt("failurePenalty", 5),
                ["timeBetweenUseMinutes"] = context.Inventory.CardConfig.GetInt("timeBetweenUseMinutes", 15)
            });
        return Result(new CardUsePlan(
            CardUseStatus.Active,
            true,
            "Cupid đang chờ kết quả finalized tiếp theo của đội được chọn.",
            effect));
    }
}

public sealed class EngineerCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Engineer;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken) =>
        Result(CreateBoothBonusPlan(context, "Engineer"));

    internal static CardUsePlan CreateBoothBonusPlan(CardUseContext context, string cardName)
    {
        var effect = CreateEffect(
            context,
            CardEffectEventCodes.BoothResultFinalized,
            targetTeamId: context.TeamId.ToString(),
            data: new BsonDocument
            {
                ["requiredBoothType"] = context.Inventory.CardConfig.GetString("requiredBoothType"),
                ["scoreMultiplier"] = context.Inventory.CardConfig.GetDouble("scoreMultiplier", 2),
                ["qualificationMode"] = context.Inventory.CardConfig.GetString("qualificationMode", "any_success")
            });
        return new CardUsePlan(
            CardUseStatus.Active,
            true,
            $"{cardName} đang chờ booth phù hợp tiếp theo.",
            effect);
    }
}

public sealed class AthleteCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Athlete;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken) =>
        Result(EngineerCardUseHandler.CreateBoothBonusPlan(context, "Athlete"));
}

public sealed class ReviveCardUseHandler(IBoothRepository boothRepository) : EffectCardUseHandler
{
    public override string CardId => CardIds.Revive;

    public override async Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        var boothId = RequireGuid(context.Inputs, "boothId", "Revive cần boothId hiện tại hợp lệ.");
        var booth = await boothRepository.GetActiveByTeamAndRaceAsync(
            context.TeamId, context.RaceId, cancellationToken);
        if (booth is null || booth.Id != boothId || booth.Status != BoothConstants.BoothStatus.Occupied)
            throw new ApplicationConflictException("Revive chỉ dùng khi đội đang chơi booth chưa kết thúc.");
        if (context.TeamCard.CardUses.Any(use => use.Status == CardUseStatus.Pending))
            throw new ApplicationConflictException("Yêu cầu Revive trước đang chờ quản trạm xác nhận.");

        var effect = CreateEffect(
            context,
            CardEffectEventCodes.ReviveOperatorConfirmation,
            targetTeamId: context.TeamId.ToString(),
            targetBoothId: boothId.ToString());
        return new CardUsePlan(
            CardUseStatus.Pending,
            false,
            "Đã gửi yêu cầu Revive; thẻ sẽ được tiêu thụ khi quản trạm xác nhận.",
            effect,
            Notifications:
            [
                new(
                    "all_organizers",
                    null,
                    "Có đội vừa gửi yêu cầu sử dụng Revive. Quản trạm phụ trách vui lòng xác nhận trên màn chấm điểm.")
            ]);
    }
}

public sealed class ScoutCardUseHandler(ISecretMissionRepository missionRepository) : EffectCardUseHandler
{
    public override string CardId => CardIds.Scout;

    public override async Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        var operationId = $"card-scout:{context.TeamCard.CardInfo.CardInstanceId}";
        var mission = await missionRepository.TryAssignRandomTechCacheAsync(
            context.RaceId,
            context.TeamId,
            operationId,
            context.OccurredAt,
            cancellationToken);
        if (mission is null)
        {
            return new CardUsePlan(
                CardUseStatus.Resolved,
                true,
                "Không còn Tech Cache nào chưa được nhận; Scout vẫn đã được sử dụng.",
                Result: new BsonDocument("missionAvailable", false));
        }

        var result = new BsonDocument
        {
            ["missionAvailable"] = true,
            ["missionId"] = mission.Id.ToString(),
            ["missionName"] = mission.Name,
            ["location"] = mission.Location ?? string.Empty
        };
        return new CardUsePlan(
            CardUseStatus.Resolved,
            true,
            $"Đã nhận Tech Cache: {mission.Name}. Vị trí: {mission.Location ?? "chưa có mô tả"}.",
            Result: result,
            Notifications:
            [
                new(
                    "team",
                    context.TeamId,
                    $"Scout đã tìm thấy Tech Cache '{mission.Name}' tại: {mission.Location ?? "chưa có mô tả"}.")
            ]);
    }
}

public sealed class InsightCardUseHandler(
    IRaceRepository raceRepository,
    ISender sender) : EffectCardUseHandler
{
    public override string CardId => CardIds.Insight;

    public override async Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        var targetTeamId = RequireGuid(
            context.Inputs,
            "targetTeamId",
            "Insight cần targetTeamId hợp lệ.");
        if (targetTeamId == context.TeamId)
            throw new ApplicationValidationException("Insight chỉ được chọn đội đối thủ.");

        var ownerScore = await raceRepository.GetRaceTeamScoreAsync(
            context.RaceId,
            context.TeamId,
            cancellationToken)
            ?? throw new ApplicationValidationException("Không tìm thấy điểm của đội sử dụng Insight.");
        var targetScore = await raceRepository.GetRaceTeamScoreAsync(
            context.RaceId,
            targetTeamId,
            cancellationToken)
            ?? throw new ApplicationValidationException("Không tìm thấy điểm của đội mục tiêu.");
        var bonusPoints = context.Inventory.CardConfig.GetInt("bonusPoints", 25);
        var eventId = $"card-insight:{context.TeamCard.CardInfo.CardInstanceId}";
        var existingLogs = await raceRepository.GetScoringLogsByEventIdAsync(
            context.RaceId,
            eventId,
            cancellationToken);
        var existing = existingLogs.FirstOrDefault(log => log.TeamId == context.TeamId);
        var appliedBonus = existing?.Delta ?? 0;

        if (existing is null && ownerScore < targetScore)
        {
            var mutation = await sender.Send(new UpdateTeamScoreCommand
            {
                RaceId = context.RaceId,
                TeamId = context.TeamId,
                EventId = eventId,
                Delta = bonusPoints,
                Reason = $"Insight so sánh điểm với đội {targetTeamId:D}"
            }, cancellationToken);
            appliedBonus = mutation?.Delta
                ?? throw new ApplicationValidationException("Không thể cộng điểm Insight cho đội.");
        }

        return new CardUsePlan(
            CardUseStatus.Resolved,
            true,
            appliedBonus > 0
                ? $"Đội mục tiêu có {targetScore} CD; đội bạn được cộng {appliedBonus} CD."
                : $"Đội mục tiêu có {targetScore} CD; đội bạn không nhận thêm CD.",
            Result: new BsonDocument
            {
                ["targetTeamId"] = targetTeamId.ToString(),
                ["targetScore"] = targetScore,
                ["ownerScoreBefore"] = ownerScore,
                ["bonusApplied"] = appliedBonus > 0,
                ["bonusPoints"] = appliedBonus
            });
    }
}

public sealed class SwapCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Swap;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        var targetTeamId = RequireGuid(context.Inputs, "targetTeamId", "Swap cần targetTeamId hợp lệ.");
        if (targetTeamId == context.TeamId)
            throw new ApplicationValidationException("Swap chỉ được chọn đội đối thủ.");

        return Result(new CardUsePlan(
            CardUseStatus.Resolved,
            true,
            "Đã kích hoạt Swap; hai đội cần liên hệ BTC/GSV để xử lý mảnh bản đồ.",
            Result: new BsonDocument("targetTeamId", targetTeamId.ToString()),
            Notifications:
            [
                new("team", targetTeamId, "Đội bạn đã được chọn để xử lý Swap. Vui lòng liên hệ BTC/GSV."),
                new("all_organizers", null, "Có đội vừa kích hoạt Swap. Vui lòng hỗ trợ hai đội xử lý mảnh bản đồ.")
            ]));
    }
}

public sealed class TrapCardUseHandler : EffectCardUseHandler
{
    public override string CardId => CardIds.Trap;

    public override Task<CardUsePlan> PrepareAsync(
        CardUseContext context,
        CancellationToken cancellationToken)
    {
        var boothId = RequireGuid(context.Inputs, "boothId", "Trap cần boothId hợp lệ.");
        var effect = CreateEffect(
            context,
            CardEffectEventCodes.BoothEntryRequested,
            targetBoothId: boothId.ToString(),
            data: new BsonDocument("penaltyPoints", context.Inventory.CardConfig.GetInt("penaltyPoints", 15)));
        return Result(new CardUsePlan(CardUseStatus.Active, true, "Đã đặt Trap.", effect));
    }
}

internal static class BsonCardConfigExtensions
{
    public static int GetInt(this BsonDocument document, string key, int fallback) =>
        document.TryGetValue(key, out var value) && value.IsNumeric ? value.ToInt32() : fallback;

    public static double GetDouble(this BsonDocument document, string key, double fallback) =>
        document.TryGetValue(key, out var value) && value.IsNumeric ? value.ToDouble() : fallback;

    public static string GetString(this BsonDocument document, string key, string fallback = "") =>
        document.TryGetValue(key, out var value) && value.IsString ? value.AsString : fallback;
}
