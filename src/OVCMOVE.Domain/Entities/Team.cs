using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;
using System;

namespace OVCMOVE.Domain.Entities;

public class Team : BaseEntity
{
    public Guid UserId { get; set; }
    public int TotalScore { get; set; } = 0;
}