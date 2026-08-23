using OVCMOVE.Domain.Common;
using static OVCMOVE.Domain.Constants.WorkflowConstants;

namespace OVCMOVE.Domain.Entities;

/// <summary>Versioned no-code workflow attached to a card in a race.</summary>
public sealed class Workflow : BaseEntity
{
    public Guid CardId { get; set; }
    public Guid RaceId { get; set; }
    public string CardKey { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string Status { get; set; } = WorkflowStatus.Active;
    public int Version { get; set; }
    public string DefinitionJson { get; set; } = string.Empty;
}
