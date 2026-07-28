using System;
using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

public class BoothScoringLog : BaseEntity
{
    public Guid BoothId { get; set; }
    public Guid TeamId { get; set; }
    public Guid OrganizerId { get; set; } 
    public int ScoreGiven { get; set; }
}