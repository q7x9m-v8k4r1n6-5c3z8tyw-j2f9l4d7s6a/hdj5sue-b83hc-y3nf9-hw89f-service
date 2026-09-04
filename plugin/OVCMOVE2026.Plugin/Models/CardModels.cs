using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OVCMOVE2026.Plugin.Models;

public static class CardStatus
{
    public const string Received = "received";
    public const string Used = "used";
    public const string Deleted = "deleted";
}

public static class CardUseStatus
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}

public static class CardIds
{
    public const string Trap = "TRAP";
}

public sealed class RaceCardDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("raceid")]
    public string RaceId { get; set; } = string.Empty;

    [BsonElement("storeOpen")]
    public bool StoreOpen { get; set; }

    [BsonElement("inventory")]
    public List<CardInventoryState> Inventory { get; set; } = [];

    [BsonElement("teams")]
    public List<RaceCardTeamState> Teams { get; set; } = [];

    [BsonElement("restockSchedules")]
    public List<RestockScheduleState> RestockSchedules { get; set; } = [];

    [BsonElement("modifiedAt")]
    public DateTime ModifiedAt { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed class CardInventoryState
{
    [BsonElement("cardId")]
    public string CardId { get; set; } = string.Empty;

    [BsonElement("remainingStock")]
    public int RemainingStock { get; set; }

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
    public DateTime ReceivedAt { get; set; }

    [BsonElement("receiveReason")]
    public string ReceiveReason { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = CardStatus.Received;

    [BsonElement("disabledAt")]
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

    [BsonElement("target")]
    public Dictionary<string, string> Target { get; set; } = [];

    [BsonElement("useAt")]
    public DateTime UseAt { get; set; }

    [BsonElement("endAt")]
    public DateTime? EndAt { get; set; }

    [BsonElement("failureReason")]
    public string? FailureReason { get; set; }

    [BsonElement("card_use_count_before")]
    public int CardUseCountBefore { get; set; }

    [BsonElement("card_use_count_after")]
    public int CardUseCountAfter { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed class RestockScheduleState
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("scheduledAt")]
    public DateTime ScheduledAt { get; set; }

    [BsonElement("quantities")]
    public Dictionary<string, int> Quantities { get; set; } = [];

    [BsonElement("status")]
    public string Status { get; set; } = "pending";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("executedAt")]
    public DateTime? ExecutedAt { get; set; }

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

public sealed class CardEffectDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
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
    public DateTime StartAt { get; set; }

    [BsonElement("limitEndAt")]
    public DateTime? LimitEndAt { get; set; }

    [BsonElement("triggerAt")]
    public DateTime? TriggerAt { get; set; }

    [BsonElement("resolvedByEventCode")]
    public string? ResolvedByEventCode { get; set; }

    [BsonElement("resolvedByEventId")]
    public string? ResolvedByEventId { get; set; }

    [BsonElement("triggeredByTeamId")]
    public string? TriggeredByTeamId { get; set; }

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("data")]
    public BsonDocument Data { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("modifiedAt")]
    public DateTime ModifiedAt { get; set; }

    [BsonElement("modifiedBy")]
    public string ModifiedBy { get; set; } = string.Empty;

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonExtraElements]
    public BsonDocument ExtraElements { get; set; } = new();
}

public sealed record CardInputDefinition(string Key, string Label, string Type, bool Required, string Description);

public sealed record CardDefinition(
    string CardId,
    string CardName,
    string Description,
    decimal Price,
    string Usage,
    IReadOnlyCollection<CardInputDefinition> Inputs,
    IReadOnlyDictionary<string, string> DefaultConfig);

public sealed record CardInventoryResponse(
    string CardId,
    string CardName,
    string Description,
    decimal Price,
    int RemainingStock,
    string Usage,
    IReadOnlyCollection<CardInputDefinition> Inputs,
    IReadOnlyDictionary<string, string> Config);

public sealed record CardStoreOverviewResponse(bool StoreOpen, IReadOnlyCollection<CardInventoryResponse> Cards);

public sealed record CardTeamResponse(
    string TeamId,
    string TeamName,
    string CardInstanceId,
    string CardId,
    string CardName,
    int CardUseCountRemain,
    DateTime ReceivedAt,
    string ReceiveReason,
    string Status,
    bool CanDelete,
    DateTime? DisabledAt,
    string? DisabledReason,
    IReadOnlyCollection<CardUseState> CardUses);

public sealed record TeamCardResponse(
    string CardInstanceId,
    string CardId,
    string CardName,
    string Description,
    string Usage,
    IReadOnlyCollection<CardInputDefinition> Inputs,
    IReadOnlyDictionary<string, string> Config,
    int CardUseCountRemain,
    DateTime ReceivedAt,
    string ReceiveReason,
    string Status,
    IReadOnlyCollection<CardUseState> CardUses);

public sealed record CardUseResponse(
    string CardUseId,
    string CardInstanceId,
    string CardId,
    string CardName,
    string Status,
    DateTime UsedAt,
    string Message);
