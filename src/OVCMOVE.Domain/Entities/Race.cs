using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain entity: luu thong tin cot loi cua mot Race trong he thong.
/// </summary>
public class Race : BaseEntity
{
    public string RaceName { get; set; } = string.Empty;
    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Place { get; set; } = string.Empty;
    public string Status { get; set; } = RaceConstants.RaceStatus.Upcoming;
    public string? CoverUrl { get; set; }

    #region setting
    public bool IsToggledLeaderboard { get; set; }
    public bool IsHiddenPoint { get; set; }
    #endregion
}
