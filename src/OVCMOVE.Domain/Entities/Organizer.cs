using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;
using System;

namespace OVCMOVE.Domain.Entities;

public class Organizer : BaseEntity<Guid>
{
    public string DisplayName { get; set; } 
    public string Email { get; set; } 
    public string Role {get; set; }
    public string Status { get; set; } = RaceConstants.TeamStatus.Active;
}