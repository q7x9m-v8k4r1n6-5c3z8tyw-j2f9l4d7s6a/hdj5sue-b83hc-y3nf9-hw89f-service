using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OVCMOVE2026.Plugin.Models;

public static class CardStatus
{
    public const string Received = "received";
    public const string Used = "used";
    public const string Deleted = "deleted";
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

    // One Mongo document represents one race. Card ownership is intentionally
    // embedded here so the plugin does not add FunctionCards tables to SQL.
    [BsonElement("teams")]
    public List<RaceCardTeamState> Teams { get; set; } = [];

    [BsonElement("cardConfigs")]
    public Dictionary<string, Dictionary<string, string>> CardConfigs { get; set; } = [];

    [BsonElement("traps")]
    public List<TrapState> Traps { get; set; } = [];

    [BsonElement("restockSchedules")]
    public List<RestockScheduleState> RestockSchedules { get; set; } = [];

    [BsonElement("modifiedAt")]
    public DateTime ModifiedAt { get; set; }
}

public sealed class CardInventoryState
{
    [BsonElement("cardId")]
    public string CardId { get; set; } = string.Empty;

    [BsonElement("remainingStock")]
    public int RemainingStock { get; set; }
}

public sealed class RaceCardTeamState
{
    [BsonElement("teamId")]
    public string TeamId { get; set; } = string.Empty;

    [BsonElement("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [BsonElement("cardId")]
    public string CardId { get; set; } = string.Empty;

    [BsonElement("cardName")]
    public string CardName { get; set; } = string.Empty;

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; set; }

    [BsonElement("receiveReason")]
    public string ReceiveReason { get; set; } = string.Empty;

    [BsonElement("usedAt")]
    public DateTime? UsedAt { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = CardStatus.Received;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }

    [BsonElement("deletedReason")]
    public string? DeletedReason { get; set; }

    [BsonElement("usageInputs")]
    public Dictionary<string, string> UsageInputs { get; set; } = [];
}

public sealed class TrapState
{
    [BsonElement("cardId")]
    public string CardId { get; set; } = CardIds.Trap;

    [BsonElement("boothId")]
    public string BoothId { get; set; } = string.Empty;

    [BsonElement("placedByTeamId")]
    public string PlacedByTeamId { get; set; } = string.Empty;

    [BsonElement("placedAt")]
    public DateTime PlacedAt { get; set; }

    [BsonElement("penaltyPoints")]
    public int PenaltyPoints { get; set; }

    [BsonElement("triggeredAt")]
    public DateTime? TriggeredAt { get; set; }

    [BsonElement("triggeredByTeamId")]
    public string? TriggeredByTeamId { get; set; }
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
}

public sealed record CardInputDefinition(
    string Key,
    string Label,
    string Type,
    bool Required,
    string Description);

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

public sealed record CardStoreOverviewResponse(
    bool StoreOpen,
    IReadOnlyCollection<CardInventoryResponse> Cards);

public sealed record CardTeamResponse(
    string TeamId,
    string TeamName,
    string CardId,
    string CardName,
    DateTime ReceivedAt,
    string ReceiveReason,
    DateTime? UsedAt,
    string Status,
    bool CanDelete,
    DateTime? DeletedAt,
    string? DeletedReason,
    IReadOnlyDictionary<string, string> UsageInputs);

public sealed record TeamCardResponse(
    string CardId,
    string CardName,
    string Description,
    string Usage,
    IReadOnlyCollection<CardInputDefinition> Inputs,
    IReadOnlyDictionary<string, string> Config,
    DateTime ReceivedAt,
    string ReceiveReason,
    DateTime? UsedAt,
    string Status);

public sealed record CardUseResponse(
    string CardId,
    string CardName,
    string Status,
    DateTime UsedAt,
    string Message);
