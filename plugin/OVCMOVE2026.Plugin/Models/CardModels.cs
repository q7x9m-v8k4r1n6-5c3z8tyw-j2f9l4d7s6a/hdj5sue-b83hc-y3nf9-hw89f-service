using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OVCMOVE2026.Plugin.Common;

namespace OVCMOVE2026.Plugin.Models;

public static class CardStatus
{
    public const string PendingPurchase = "pending_purchase";
    public const string Received = "received";
    public const string Used = "used";
    public const string Deleted = "deleted";
}

public static class CardUseStatus
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string Resolved = "resolved";
    public const string Succeeded = Resolved;
    public const string Failed = "failed";
}

public static class CardIds
{
    public const string Overclock = "OVERCLOCK";
    public const string Cupid = "CUPID";
    public const string Engineer = "ENGINEER";
    public const string Athlete = "ATHLETE";
    public const string Revive = "REVIVE";
    public const string Swap = "SWAP";
    public const string Trap = "TRAP";
}

public static class CardTypes
{
    public const string CoreChip = "core_chip";
    public const string DataPatch = "data_patch";
}

public static class CardEffectEventCodes
{
    public const string BoothEntryRequested = "booth.entry.requested";
    public const string BoothResultFinalized = "booth.result.finalized";
    public const string ReviveOperatorConfirmation = "booth.revive.operator-confirmed";
    public const string OverclockResolution = "race.overclock.resolve";
}

public static class OverclockWindowStatus
{
    public const string NotOpened = "not_opened";
    public const string Closed = "closed";
    public const string Open = "open";
    public const string Resolved = "resolved";
}

public sealed class RaceCardDocument
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringOrObjectIdSerializer))]
    public string Id { get; set; } = string.Empty;

    [BsonElement("raceid")]
    public string RaceId { get; set; } = string.Empty;

    [BsonElement("inventory")]
    public List<CardInventoryState> Inventory { get; set; } = [];

    [BsonElement("teams")]
    public List<RaceCardTeamState> Teams { get; set; } = [];

    [BsonElement("storeOpen")]
    public bool StoreOpen { get; set; }

    [BsonElement("maxDataPatchPerTeam")]
    public int MaxDataPatchPerTeam { get; set; } = 3;

    [BsonElement("overclockWindow")]
    public OverclockWindowState OverclockWindow { get; set; } = new();

    [BsonElement("modifiedAt")]
    [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
    public DateTime ModifiedAt { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed class OverclockWindowState
{
    [BsonElement("status")]
    public string Status { get; set; } = OverclockWindowStatus.NotOpened;

    [BsonElement("openedAt"), BsonIgnoreIfNull]
    public DateTime? OpenedAt { get; set; }

    [BsonElement("openedBy"), BsonIgnoreIfNull]
    public string? OpenedBy { get; set; }

    [BsonElement("closedAt"), BsonIgnoreIfNull]
    public DateTime? ClosedAt { get; set; }

    [BsonElement("closedBy"), BsonIgnoreIfNull]
    public string? ClosedBy { get; set; }

    [BsonElement("resolvedAt"), BsonIgnoreIfNull]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("resolutionEventId"), BsonIgnoreIfNull]
    public string? ResolutionEventId { get; set; }
}

public sealed class CardInventoryState
{
    [BsonElement("cardId")]
    public string CardId { get; set; } = string.Empty;

    [BsonElement("remainingStock")]
    public int RemainingStock { get; set; }

    // -1 distinguishes a legacy document with no price from an intentional free card.
    [BsonElement("price")]
    public int Price { get; set; } = -1;

    [BsonElement("cardConfig")]
    public BsonDocument CardConfig { get; set; } = new();

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed class RaceCardTeamState
{
    [BsonElement("teamId")]
    public string TeamId { get; set; } = string.Empty;

    [BsonElement("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [BsonElement("card")]
    public List<TeamCardState> Cards { get; set; } = [];

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed class TeamCardState
{
    [BsonElement("cardInfo")]
    public TeamCardInfo CardInfo { get; set; } = new();

    [BsonElement("cardUse")]
    public List<CardUseState> CardUses { get; set; } = [];

    [BsonElement("receivedAt")]
    [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
    public DateTime ReceivedAt { get; set; }

    [BsonElement("receiveReason")]
    public string ReceiveReason { get; set; } = string.Empty;

    [BsonElement("purchaseId"), BsonIgnoreIfNull]
    public string? PurchaseId { get; set; }

    [BsonElement("purchasePrice"), BsonIgnoreIfNull]
    public int? PurchasePrice { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = CardStatus.Received;

    [BsonElement("nextTimeAvailable")]
    public DateTime? NextTimeAvailable { get; set; }

    [BsonElement("disabledAt")]
    [BsonSerializer(typeof(FlexibleNullableDateTimeSerializer))]
    public DateTime? DisabledAt { get; set; }

    [BsonElement("disabledReason")]
    public string? DisabledReason { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed class TeamCardInfo
{
    [BsonElement("cardInstanceId")]
    public string CardInstanceId { get; set; } = string.Empty;

    [BsonElement("cardId")]
    public string CardId { get; set; } = string.Empty;

    [BsonElement("card_use_count_remain")]
    public int CardUseCountRemain { get; set; } = 1;

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed class CardUseState
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("effectId")]
    public string? EffectId { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = CardUseStatus.Succeeded;

    [BsonElement("inputs")]
    public BsonDocument Inputs { get; set; } = new();

    [BsonElement("useAt")]
    [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
    public DateTime UseAt { get; set; }

    [BsonElement("endAt")]
    [BsonSerializer(typeof(FlexibleNullableDateTimeSerializer))]
    public DateTime? EndAt { get; set; }

    [BsonElement("failureReason")]
    public string? FailureReason { get; set; }

    [BsonElement("result")]
    public BsonDocument? Result { get; set; }

    [BsonElement("card_use_count_before")]
    public int CardUseCountBefore { get; set; }

    [BsonElement("card_use_count_after")]
    public int CardUseCountAfter { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public static class CardEffectStatus
{
    public const string Active = "active";
    public const string Resolved = "resolved";
    public const string Expired = "expired";
    public const string Blocked = "blocked";
}

public static class CardEffectResolutionCodes
{
    public const string OperatorConfirmed = "operator_confirmed";
    public const string OperatorRejected = "operator_rejected";
}

public sealed class CardEffectDocument
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringOrObjectIdSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("raceId")]
    public string RaceId { get; set; } = string.Empty;

    [BsonElement("cardId")]
    public string CardId { get; set; } = string.Empty;

    [BsonElement("cardInstanceId")]
    public string CardInstanceId { get; set; } = string.Empty;

    [BsonElement("cardUseId")]
    public string CardUseId { get; set; } = string.Empty;

    [BsonElement("ownerTeamId")]
    public string OwnerTeamId { get; set; } = string.Empty;

    [BsonElement("targetBoothId")]
    public string? TargetBoothId { get; set; }

    [BsonElement("targetTeamId")]
    public string? TargetTeamId { get; set; }

    [BsonElement("triggerEventCode")]
    public string TriggerEventCode { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = CardEffectStatus.Active;

    [BsonElement("resolution")]
    public string? Resolution { get; set; }

    [BsonElement("remainingTriggers")]
    public int RemainingTriggers { get; set; } = 1;

    [BsonElement("startAt")]
    [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
    public DateTime StartAt { get; set; }

    [BsonElement("limitEndAt")]
    [BsonSerializer(typeof(FlexibleNullableDateTimeSerializer))]
    public DateTime? LimitEndAt { get; set; }

    [BsonElement("triggerAt")]
    [BsonSerializer(typeof(FlexibleNullableDateTimeSerializer))]
    public DateTime? TriggerAt { get; set; }

    [BsonElement("claimedAt")]
    [BsonIgnoreIfNull]
    public DateTime? ClaimedAt { get; set; }

    [BsonElement("claimedByEventId")]
    [BsonIgnoreIfNull]
    public string? ClaimedByEventId { get; set; }

    [BsonElement("resolvedByEventCode")]
    public string? ResolvedByEventCode { get; set; }

    [BsonElement("resolvedByEventId")]
    public string? ResolvedByEventId { get; set; }

    [BsonElement("triggeredByTeamId")]
    public string? TriggeredByTeamId { get; set; }

    [BsonElement("resolvedAt")]
    [BsonSerializer(typeof(FlexibleNullableDateTimeSerializer))]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("data")]
    public BsonDocument Data { get; set; } = new();

    [BsonElement("createdAt")]
    [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
    public DateTime CreatedAt { get; set; }

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("modifiedAt")]
    [BsonSerializer(typeof(FlexibleDateTimeSerializer))]
    public DateTime ModifiedAt { get; set; }

    [BsonElement("modifiedBy")]
    public string ModifiedBy { get; set; } = string.Empty;

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed record CardEffectResolution(
    string EffectId,
    string Resolution,
    BsonDocument Result,
    DateTime? NextTimeAvailable = null);

public sealed record CardInputDefinition(string Key, string Label, string Type, bool Required, string Description);

public sealed record CardDefinition(
    string CardId,
    string CardName,
    string CardType,
    string Description,
    decimal Price,
    string Usage,
    IReadOnlyCollection<CardInputDefinition> Inputs,
    BsonDocument DefaultConfig);

public sealed record CardInventoryResponse(
    string CardId,
    string CardName,
    string CardType,
    string Description,
    decimal Price,
    int RemainingStock,
    int MaxStock,
    string Usage,
    IReadOnlyCollection<CardInputDefinition> Inputs,
    IReadOnlyDictionary<string, object?> Config);

public sealed record CardStoreOverviewResponse(IReadOnlyCollection<CardInventoryResponse> Cards);

public sealed record CardShopStateResponse(
    bool StoreOpen,
    int MaxDataPatchPerTeam);

public sealed record CardShopItemResponse(
    string CardId,
    string CardName,
    string Description,
    int Price,
    int RemainingStock,
    string Usage,
    IReadOnlyCollection<CardInputDefinition> Inputs);

public sealed record TeamCardShopResponse(
    bool StoreOpen,
    int MaxDataPatchPerTeam,
    int PurchasedCount,
    int RemainingSlots,
    IReadOnlyCollection<CardShopItemResponse> Cards);

public sealed record CardPurchaseResponse(
    string PurchaseId,
    string EventId,
    string CardInstanceId,
    string CardId,
    int Price,
    int ScoreBefore,
    int ScoreAfter,
    int RemainingStock,
    string Status,
    string Message);

public sealed record OverclockWindowResponse(
    string Status,
    DateTime? OpenedAt,
    string? OpenedBy,
    DateTime? ClosedAt,
    string? ClosedBy,
    DateTime? ResolvedAt,
    string? ResolutionEventId);

public sealed record OverclockResolutionResponse(
    string Status,
    int PredictionCount,
    int CorrectCount,
    int IncorrectCount,
    int NotEvaluatedCount);

public sealed record CardUseHistoryResponse(
    string CardUseId,
    string? EffectId,
    string Status,
    IReadOnlyDictionary<string, object?> Inputs,
    DateTime UsedAt,
    DateTime? EndAt,
    string? FailureReason,
    IReadOnlyDictionary<string, object?>? Result);

public sealed record CardAvailabilityResponse(
    bool CanUse,
    string ReasonCode,
    string Reason,
    DateTime? NextTimeAvailable);

public sealed record CardTeamResponse(
    string TeamId,
    string TeamName,
    string CardInstanceId,
    string CardId,
    string CardName,
    string CardType,
    int CardUseCountRemain,
    DateTime ReceivedAt,
    string ReceiveReason,
    string Status,
    bool CanDelete,
    DateTime? DisabledAt,
    string? DisabledReason,
    IReadOnlyCollection<CardUseHistoryResponse> CardUses);

public sealed record TeamCardResponse(
    string CardInstanceId,
    string CardId,
    string CardName,
    string CardType,
    string Description,
    string Usage,
    IReadOnlyCollection<CardInputDefinition> Inputs,
    IReadOnlyDictionary<string, object?> Config,
    int CardUseCountRemain,
    DateTime ReceivedAt,
    string ReceiveReason,
    string Status,
    CardAvailabilityResponse Availability,
    IReadOnlyCollection<CardUseHistoryResponse> CardUses);

public sealed record CardUseResponse(
    string CardUseId,
    string? EffectId,
    string CardInstanceId,
    string CardId,
    string CardName,
    string Status,
    DateTime UsedAt,
    DateTime? EndAt,
    string Message);
