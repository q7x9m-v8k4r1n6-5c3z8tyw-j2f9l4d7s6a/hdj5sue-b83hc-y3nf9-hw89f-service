namespace OVCMOVE.Application.Features.Organizers.Command.ChangeOrganizerStatus;

public class OrganizerStatusResponse
{
    public Guid OrganizerId { get; set; }
    public string Status { get; set; } = string.Empty;
}
