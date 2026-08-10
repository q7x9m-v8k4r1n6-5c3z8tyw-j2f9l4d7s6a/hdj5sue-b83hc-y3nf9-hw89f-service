using System.Text.Json;

namespace OVCMOVE.Application.Features.CardEffects;

public sealed record CardEffectEvent
{
    public Guid EventId { get; init; }
    public string Type { get; init; } = string.Empty;
    public Guid RaceId { get; init; }
    public Guid? ActorTeamId { get; init; }
    public Guid? TargetTeamId { get; init; }
    public Guid? BoothId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public JsonElement Data { get; init; }
}

public sealed record CardEffectRuntimeState
{
    public Guid EffectInstanceId { get; init; }
    public Guid OwnerTeamId { get; init; }
    public int RemainingUses { get; init; }
    public string Status { get; init; } = CardEffectStatus.Active;
    public DateTimeOffset? ExpiresAt { get; init; }
    public IReadOnlySet<string> DistinctValues { get; init; } =
        new HashSet<string>(StringComparer.Ordinal);
}

public static class CardEffectStatus
{
    public const string Active = "active";
    public const string Consumed = "consumed";
    public const string Expired = "expired";
    public const string Cancelled = "cancelled";
}

public sealed record CardEffectExecutionContext(
    CardEffectWorkflowDefinition Definition,
    CardEffectEvent Event,
    CardEffectRuntimeState State);

public sealed record CardEffectTraceEntry(
    string StepType,
    string HandlerType,
    bool Succeeded,
    string? Message = null);

public sealed record CardEffectExecutionResult
{
    public string Outcome { get; init; } = CardEffectOutcome.Ignored;
    public bool CancelCurrentAction { get; init; }
    public CardEffectRuntimeState State { get; init; } = new();
    public IReadOnlyList<CardEffectTraceEntry> Trace { get; init; } = [];
}

public static class CardEffectOutcome
{
    public const string Ignored = "ignored";
    public const string ConditionsNotMet = "conditions_not_met";
    public const string Succeeded = "succeeded";
    public const string ActionFailed = "action_failed";
}

