using MediatR;
using OVCMOVE.Application.DTOs.Organizer;

namespace OVCMOVE.Application.Features.Organizer.Command.ChangeOrganizerStatus;

public class ChangeOrganizerStatusCommand : IRequest<OrganizerStatusResponse?>
{
    public Guid OrganizerId { get; init; }

    public string Status { get; init; } = string.Empty;
}
