using MediatR;
using OVCMOVE.Application.DTOs.Organizer;

namespace OVCMOVE.Application.Organizers.Commands;

public class CreateOrganizerCommand : IRequest<OrganizerResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}