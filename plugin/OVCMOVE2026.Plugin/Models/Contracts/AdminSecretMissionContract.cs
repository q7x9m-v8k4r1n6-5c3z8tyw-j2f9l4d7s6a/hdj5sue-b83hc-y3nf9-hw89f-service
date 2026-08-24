namespace OVCMOVE2026.Plugin.Models.Contracts;

public class CreateSecretMissionRequest
{
    public Guid RaceId { get; set; }
    public Guid TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}