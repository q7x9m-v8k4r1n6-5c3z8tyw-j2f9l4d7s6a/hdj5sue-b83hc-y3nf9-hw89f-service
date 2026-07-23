using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;
using System;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain.Entities: các thông tin của role là Team kèm UserId
/// </summary>
public class Team : BaseEntity
{
    public Guid UserId { get; set; }
    public int TotalScore { get; set; } = 0;
}