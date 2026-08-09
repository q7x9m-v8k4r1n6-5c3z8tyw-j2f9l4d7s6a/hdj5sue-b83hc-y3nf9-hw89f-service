namespace OVCMOVE2026.Plugin.Models.DTOs;

public class SecretMissionDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public List<EvidenceFileDto>? EvidenceImageUrls { get; set; }
    public List<EvidenceFileDto>? EvidenceVideoUrls { get; set; }
}