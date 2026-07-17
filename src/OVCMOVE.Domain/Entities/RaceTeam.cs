
using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain entity: quan he giua Race va Team tham gia.
/// </summary>
public class RaceTeam : BaseEntity<Guid>
{
    public Guid RaceID { get; set; }
    public Guid TeamID { get; set; }
}
