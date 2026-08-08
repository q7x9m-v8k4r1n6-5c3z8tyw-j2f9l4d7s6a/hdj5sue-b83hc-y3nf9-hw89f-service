using OVCMOVE.Domain.Common;

namespace OVCMOVE2026.Plugin.Models;

/// <summary>
/// Domain entity: nhiệm vụ ẩn ban đầu hoặc hộp mù dọc đường 
/// </summary>
public class SecretMission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// true: đã có team nhận nvbm này / false: chưa team nào tìm thấy hộp mù
    /// </summary>
    public bool IsAssigned { get; set;}
    public string? Location { get; set; }
    public Guid? TeamId {get; set;}
    public Guid? ReceivedBy { get; set; }
    public DateTime? ReceivedTime { get; set; }
    public Guid? SubmittedBy { get; set; }
    public DateTime? SubmittedTime { get; set; }
    public string? QrCodeUrl { get; set; }
    public List<string>? EvidenceVideoUrl { get; set; }
    public List<string>? EvidenceImageUrl { get; set; }

}