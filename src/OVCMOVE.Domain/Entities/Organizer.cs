using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Enums;

namespace OVCMOVE.Domain.Entities;

public class Organizer : BaseEntity<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public OrganizerStatus Status { get; set; } = OrganizerStatus.Active;
}