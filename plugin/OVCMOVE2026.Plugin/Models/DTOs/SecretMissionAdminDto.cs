namespace OVCMOVE2026.Plugin.Models.DTOs;

public class SecretMissionAdminOverviewDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
    public bool HasImageEvidence { get; set; }
    public bool HasVideoEvidence { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

public class SecretMissionAdminDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
    public List<EvidenceFileDto> EvidenceImageUrls { get; set; } = new();
    public List<EvidenceFileDto> EvidenceVideoUrls { get; set; } = new();
}