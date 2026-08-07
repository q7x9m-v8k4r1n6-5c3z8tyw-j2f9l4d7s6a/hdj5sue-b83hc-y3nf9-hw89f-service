using OVCMOVE.Domain.Common;

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
    public string Status { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public string? Rules { get; set; }

    public bool IsToggledLeaderboard { get; set; }
    public bool IsHiddenPoint { get; set; }
}
