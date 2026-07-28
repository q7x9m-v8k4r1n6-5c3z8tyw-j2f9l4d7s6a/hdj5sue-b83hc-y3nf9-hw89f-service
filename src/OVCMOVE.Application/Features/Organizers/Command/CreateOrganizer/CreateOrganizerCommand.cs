using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Organizers.Command.CreateOrganizer;

public class CreateOrganizerCommand : AuditedRequest, IRequest<OrganizerResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
