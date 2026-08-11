using System;

namespace OVCMOVE2026.Plugin.Models.DTOs;

public class SecretMissionOverviewDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    
    public bool HasImageEvidence { get; set; }
    public bool HasVideoEvidence { get; set; }
}