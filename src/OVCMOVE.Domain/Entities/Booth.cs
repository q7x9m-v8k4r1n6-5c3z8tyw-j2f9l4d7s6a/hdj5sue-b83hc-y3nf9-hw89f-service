using System;
using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Domain entity: tram/checkpoint thuoc ve mot Race.
/// </summary>
public class Booth : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BoothOrganizerID { get; set; } = string.Empty;
    public Guid RaceID { get; set; }
    public bool IsHidden { get; set; } = false;
    public string status {  get; set; } = BoothConstants.BoothStatus.Free;
}
