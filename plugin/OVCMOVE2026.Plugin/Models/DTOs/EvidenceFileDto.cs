namespace OVCMOVE2026.Plugin.Models.DTOs;

public class EvidenceFileDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}