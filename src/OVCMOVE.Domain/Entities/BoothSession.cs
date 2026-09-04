using System;
using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Domain.Entities;

public sealed class BoothSession : BaseEntity 
{
    public Guid RaceId { get; set; }
    public Guid BoothId { get; set; }
    public Guid TeamId { get; set; }
    public string? Result { get; set; }

    public int? BaseReward { get; set; }
    public int BonusReward { get; set; }
    public string BonusRewardReason { get; set; } = string.Empty;
    public int? FinalReward { get; set; }

    //public bool FirstAttemptQualified { get; set; } // athlete có vượt qua vòng loại trong lần thử đầu tiên hay không, checkbox quan tram 
}