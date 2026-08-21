using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>Immutable audit record for one workflow execution.</summary>
public sealed class WorkflowRun : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public Guid RaceId { get; set; }
    public string CardKey { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string? EventId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsSimulation { get; set; }
    public string InputJson { get; set; } = string.Empty;
    public string OutputJson { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
