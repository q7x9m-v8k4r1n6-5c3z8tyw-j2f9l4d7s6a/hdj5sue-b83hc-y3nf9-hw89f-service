using OVCMOVE.Domain.Common;
using OVCMOVE2026.Plugin.Models.Constants;

namespace OVCMOVE2026.Plugin.Models;

/// <summary>
/// Domain entity: nhiệm vụ ẩn ban đầu hoặc hộp mù dọc đường 
/// </summary>
public class ScretMission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// available: chưa ai nhận / inProgress: nhận rồi / completed: đã hoàn thành
    /// </summary>
    public string Status { get; set; }  = ScretMissionConstants.Status.Available;
    public string? Location { get; set; }
    public Guid? TeamId {get; set;}
    public Guid? ReceivedBy { get; set; }
    public DateTime? ReceivedTime { get; set; }
    public Guid? SubmittedBy { get; set; }
    public DateTime? SubmittedTime { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? EvidenceVideoUrl { get; set; }
    public string? EvidenceImageUrl { get; set; }

}