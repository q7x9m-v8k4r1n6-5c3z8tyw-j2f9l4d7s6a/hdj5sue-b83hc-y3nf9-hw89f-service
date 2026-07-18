namespace OVCMOVE.Application.DTOs.Organizer;

public class OrganizerStatusResponse
{
    public Guid OrganizerId { get; set; }
    public string Status { get; set; } = string.Empty;
}
