using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Stores the relationship between a race and an organizer.
/// </summary>
public class RaceOrganizer : BaseEntity
{
    public Guid RaceId { get; set; }
    public Guid OrganizerId { get; set; }
}
