using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>A configurable function card owned by a race.</summary>
public sealed class FunctionCard : BaseEntity
{
    public Guid RaceId { get; set; }
    public Guid? TeamId { get; set; }
    public string CardKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? BackgroundUrl { get; set; }
    public string InputsJson { get; set; } = "[]";
}
