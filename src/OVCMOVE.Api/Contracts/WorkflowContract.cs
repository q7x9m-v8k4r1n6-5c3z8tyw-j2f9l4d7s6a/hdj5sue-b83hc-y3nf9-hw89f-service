using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OVCMOVE.Api.Contracts;

public static class WorkflowContract
{
    public class CreateWorkflowRequest
    {
        [Required] public Guid CardId { get; init; }
        [Required, MaxLength(255)] public string Name { get; init; } = string.Empty;
        [MaxLength(1000)] public string Description { get; init; } = string.Empty;
        [Required] public string TriggerType { get; init; } = string.Empty;
        [Required] public JsonElement Definition { get; init; }
    }

    public sealed class UpdateWorkflowRequest : CreateWorkflowRequest
    {
        [Required] public DateTime ExpectedModifiedAt { get; init; }
    }

    public sealed class ChangeWorkflowStatusRequest
    {
        [Required] public DateTime ExpectedModifiedAt { get; init; }
        [Required] public string Status { get; init; } = string.Empty;
    }

    public sealed class ExecuteWorkflowRequest
    {
        public bool IsSimulation { get; init; }
        [MaxLength(100)] public string? EventId { get; init; }
        public Guid? ActorTeamId { get; init; }
        public Guid? TargetTeamId { get; init; }
        public Dictionary<string, JsonElement> Variables { get; init; } = [];
        public JsonElement Payload { get; init; }
    }
}
