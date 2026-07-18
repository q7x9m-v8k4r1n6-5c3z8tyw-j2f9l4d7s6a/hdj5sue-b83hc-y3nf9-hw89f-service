using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;
using System;

namespace OVCMOVE.Domain.Entities;

public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string LeaderEmail { get; set; }
    public string Username { get; set; }
    public string Status { get; set; } = RaceConstants.TeamStatus.Active;
}