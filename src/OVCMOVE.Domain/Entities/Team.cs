using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;
using System;

namespace OVCMOVE.Domain.Entities;

public class Team : BaseEntity
{
    public Guid UserId { get; set; }
    public int TotalScore { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LeaderEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = RaceConstants.TeamStatus.Active;
}
