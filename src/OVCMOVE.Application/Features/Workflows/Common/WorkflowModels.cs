using System.Text.Json;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Workflows.Common;

public static class WorkflowConstants
{
    public static class Trigger
    {
        public const string Activated = "activated";
        public const string Attacked = "attacked";
    }

    public static class Status
    {
        public const string Draft = "draft";
        public const string Published = "published";
        public const string Disabled = "disabled";
    }

    public static class RunStatus
    {
        public const string Running = "running";
        public const string Succeeded = "succeeded";
        public const string Failed = "failed";
        public const string Canceled = "canceled";
    }

    public static class TraceStatus
    {
        public const string Succeeded = "succeeded";
        public const string Failed = "failed";
    }

    public static class NodeType
    {
        public const string TriggerActivated = "trigger.activated";
        public const string TriggerAttacked = "trigger.attacked";
        public const string Condition = "logic.condition";
        public const string CreateVariable = "data.create_variable";
        public const string SetVariable = "data.set_variable";
        public const string RandomNumber = "data.random_number";
        public const string ReadInputValue = "input.read_value";
        public const string AdjustScore = "team.adjust_score";
        public const string Attack = "attack.execute";
        public const string SendMessage = "notify.send_message";
        public const string ApplyCardEffect = "card.apply_effect";
        public const string Scope = "flow.scope";
        public const string Stop = "flow.stop";
    }
}

public sealed class WorkflowDefinitionModel
{
    public int SchemaVersion { get; init; } = 1;
    public IReadOnlyCollection<WorkflowNodeModel> Nodes { get; init; } = [];
    public IReadOnlyCollection<WorkflowEdgeModel> Edges { get; init; } = [];
}

public sealed class WorkflowNodeModel
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public WorkflowPositionModel Position { get; init; } = new();
    public JsonElement Config { get; init; }
}

public sealed class WorkflowPositionModel
{
    public double X { get; init; }
    public double Y { get; init; }
}

public sealed class WorkflowEdgeModel
{
    public string Id { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string? SourceHandle { get; init; }
}

public sealed record WorkflowCatalogItemModel(
    string Type,
    string Category,
    string Label,
    string Description,
    bool IsTrigger,
    JsonElement DefaultConfig);

public sealed record WorkflowResultModel(
    Guid Id,
    Guid CardId,
    Guid RaceId,
    string CardKey,
    string CardName,
    string Name,
    string Description,
    string TriggerType,
    string Status,
    int Version,
    WorkflowDefinitionModel Definition,
    DateTime CreatedAt,
    DateTime ModifiedAt);

public sealed record WorkflowRunResultModel(
    Guid Id,
    Guid WorkflowId,
    string Status,
    bool IsSimulation,
    string? EventId,
    JsonElement Input,
    JsonElement Output,
    string? Error,
    DateTime StartedAt,
    DateTime? CompletedAt);

public sealed class WorkflowExecutionInputModel
{
    public string? EventId { get; init; }
    public Guid? ActorTeamId { get; init; }
    public Guid? TargetTeamId { get; init; }
    public IReadOnlyDictionary<string, JsonElement> Variables { get; init; } =
        new Dictionary<string, JsonElement>();
    public JsonElement Payload { get; init; } = WorkflowJson.ToElement(new { });
}

public sealed record WorkflowTraceItemModel(
    string NodeId,
    string NodeType,
    string Status,
    string Detail);

public sealed record WorkflowEffectModel(
    string Type,
    string Target,
    JsonElement Data,
    bool Applied);

public sealed record WorkflowExecutionResultModel(
    Guid RunId,
    string Status,
    bool IsSimulation,
    IReadOnlyCollection<WorkflowTraceItemModel> Trace,
    IReadOnlyCollection<WorkflowEffectModel> Effects,
    IReadOnlyDictionary<string, JsonElement> Variables,
    bool RealtimeSynced = true,
    string? Message = null);

public static class WorkflowJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static WorkflowDefinitionModel DeserializeDefinition(string json) =>
        JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, Options)
        ?? throw new JsonException("Workflow definition is empty.");

    public static WorkflowResultModel ToResult(this Workflow workflow) => new(
        workflow.Id,
        workflow.CardId,
        workflow.RaceId,
        workflow.CardKey,
        workflow.CardName,
        workflow.Name,
        workflow.Description,
        workflow.TriggerType,
        workflow.Status,
        workflow.Version,
        DeserializeDefinition(workflow.DefinitionJson),
        workflow.CreatedAt,
        workflow.ModifiedAt);

    public static WorkflowRunResultModel ToResult(this WorkflowRun run) => new(
        run.Id,
        run.WorkflowId,
        run.Status,
        run.IsSimulation,
        run.EventId,
        ParseElement(run.InputJson),
        ParseElement(run.OutputJson),
        run.Error,
        run.StartedAt,
        run.CompletedAt);

    public static JsonElement ToElement<T>(T value) =>
        JsonSerializer.SerializeToElement(value, Options);

    public static JsonElement ParseElement(string json)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.Clone();
    }
}
