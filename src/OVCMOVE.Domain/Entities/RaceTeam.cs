
using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain entity: quan he giua Race va Team tham gia.
/// </summary>
public class RaceTeam : BaseEntity
{
    public Guid RaceID { get; set; }
    public Guid TeamID { get; set; }
}
