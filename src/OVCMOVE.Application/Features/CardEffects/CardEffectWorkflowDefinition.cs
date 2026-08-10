using System.Text.Json;

namespace OVCMOVE.Application.Features.CardEffects;

public sealed record CardEffectWorkflowDefinition
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid WorkflowVersionId { get; init; }
    public int Version { get; init; } = 1;
    public string TriggerType { get; init; } = string.Empty;
    public IReadOnlyList<CardEffectStepDefinition> Conditions { get; init; } = [];
    public IReadOnlyList<CardEffectStepDefinition> Actions { get; init; } = [];
    public CardEffectExecutionPolicy Policy { get; init; } = new();
}

public sealed record CardEffectStepDefinition
{
    public string Type { get; init; } = string.Empty;
    public JsonElement Config { get; init; }
}

public sealed record CardEffectExecutionPolicy
{
    public int MaximumTriggers { get; init; } = 1;
    public string ConsumeWhen { get; init; } =
        CardEffectConsumeWhen.ActionSucceeded;
    public int? ExpiresAfterSeconds { get; init; }
    public string? DistinctBy { get; init; }
    public int? MaximumDistinctValues { get; init; }
}

public static class CardEffectConsumeWhen
{
    public const string ActionSucceeded = "action_succeeded";
    public const string Never = "never";

    public static IReadOnlySet<string> Supported { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            ActionSucceeded,
            Never
        };
}

