namespace OVCMOVE.Application.DTOs.Organizer;

public class CreateOrganizerRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}