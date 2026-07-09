namespace OVCMOVE.Application.DTOs.Organizer;

public class OrganizerResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public OVCMOVE.Domain.Enums.OrganizerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}