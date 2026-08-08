using System;
using System.Collections.Generic;

namespace OVCMOVE2026.Plugin.Models.DTOs;

public class SecretMissionDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    
    public List<string>? EvidenceImageUrls { get; set; }
    public List<string>? EvidenceVideoUrls { get; set; }
    public DateTime? SubmittedTime { get; set; }
}