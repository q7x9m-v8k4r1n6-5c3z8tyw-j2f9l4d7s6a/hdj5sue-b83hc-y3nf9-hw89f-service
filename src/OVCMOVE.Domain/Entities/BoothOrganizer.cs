using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>Stores the relationship between a booth and an organizer.</summary>
public class BoothOrganizer : BaseEntity
{
    public Guid BoothId { get; set; }
    public Guid OrganizerId { get; set; }
}
