using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

public class Booth : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid BoothOrganizerId { get; set; } 
    public Guid RaceId { get; set; }
    public bool IsHidden { get; set; } = false;
    public string Status { get; set; } = string.Empty;
}