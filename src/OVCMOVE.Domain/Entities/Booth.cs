using OVCMOVE.Domain.Common;
using static OVCMOVE.Domain.Constants.BoothConstants;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Stores one booth belonging to a race.
/// </summary>
public class Booth : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid RaceId { get; set; }
    public Guid? TeamId { get; set; } // Team đang chiếm/giữ trạm
    public bool IsHidden { get; set; } = false;
    public string Type { get; set; } = BoothType.Other;
    public int? MaximumScore { get; set; }
    public string Status { get; set; } = BoothStatus.Free;
}
