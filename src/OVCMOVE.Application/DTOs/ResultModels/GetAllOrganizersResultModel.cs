using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;

public class GetAllOrganizersResultModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = OrganizerConstants.OrganizerStatus.Active;
}