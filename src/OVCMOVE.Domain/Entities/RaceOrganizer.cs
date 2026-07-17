using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain entity: quan he giua Race va Organizer/BTC.
/// </summary>
public class RaceOrganizer : BaseEntity<Guid>
{
    public Guid RaceID { get; set; }
    public Guid OrganizerID { get; set; }
}
