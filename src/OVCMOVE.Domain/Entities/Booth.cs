using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain entity: tram/checkpoint thuoc ve mot Race.
/// </summary>
public class Booth : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BoothOrganizerID { get; set; } = string.Empty;
    public Guid RaceID { get; set; }
}
