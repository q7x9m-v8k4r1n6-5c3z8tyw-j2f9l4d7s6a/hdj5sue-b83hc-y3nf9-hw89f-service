using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Stores one booth belonging to a race.
/// </summary>
public class Booth : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid RaceId { get; set; }
    public Guid? TeamId {  get; set; }
    public bool IsHidden { get; set; } = false;
    public string Status { get; set; } = string.Empty;
}
