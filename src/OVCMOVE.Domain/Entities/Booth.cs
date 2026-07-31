using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Domain.Entities;

public class Booth : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid RaceId { get; set; }
    public Guid? TeamId {  get; set; }
    public bool IsHidden { get; set; } = false;
    public string Status { get; set; } = BoothConstants.BoothStatus.Free;
}