using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Domain.Entities;

public class Organizer : BaseEntity
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = OrganizerConstants.OrganizerStatus.Active;
}
