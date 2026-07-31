using System;
using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

public class BoothScoringLog : BaseEntity
{
    public Guid RaceId { get; set; }
    public Guid TeamId { get; set; }
    public Guid? BoothId { get; set; }
    public Guid? OrganizerId { get; set; }
    public int ScoreGiven { get; set; }
    public int ScoreAfterChange { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
