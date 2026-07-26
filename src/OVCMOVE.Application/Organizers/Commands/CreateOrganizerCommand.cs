using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Organizer;

namespace OVCMOVE.Application.Organizers.Commands;

public class CreateOrganizerCommand : BaseRequestModel, IRequest<OrganizerResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}