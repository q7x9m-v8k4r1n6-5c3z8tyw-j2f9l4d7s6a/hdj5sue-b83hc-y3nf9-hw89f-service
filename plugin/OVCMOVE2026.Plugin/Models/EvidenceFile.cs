using OVCMOVE.Domain.Common;

namespace OVCMOVE2026.Plugin.Models;

public class EvidenceFile : BaseEntity
{
    public Guid MissionId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // "image" hoặc "video"
}