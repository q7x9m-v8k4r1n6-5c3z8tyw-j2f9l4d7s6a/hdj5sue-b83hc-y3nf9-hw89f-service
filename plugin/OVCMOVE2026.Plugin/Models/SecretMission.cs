using System;
using System.Collections.Generic;
using OVCMOVE.Domain.Common;

namespace OVCMOVE2026.Plugin.Models;

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
    public string? QrCodeUrl { get; set; }
    public List<EvidenceFile> Evidences { get; set; } = new();
}