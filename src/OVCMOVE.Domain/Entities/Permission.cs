using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    // Stable business capability code, independent from controller or method names.
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    // Taxonomy metadata for grouping permissions, not endpoint implementation details.
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}
