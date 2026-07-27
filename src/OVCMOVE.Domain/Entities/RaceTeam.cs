using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Stores the relationship between a race and a participating team.
/// </summary>
public class RaceTeam : BaseEntity
{
    public Guid RaceId { get; set; }
    public Guid TeamId { get; set; }
}
