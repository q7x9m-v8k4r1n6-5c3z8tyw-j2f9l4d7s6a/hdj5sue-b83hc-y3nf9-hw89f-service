using MediatR;

namespace OVCMOVE.Application.Features.Organizers.Command.ChangeOrganizerStatus;

public class ChangeOrganizerStatusCommand : IRequest<OrganizerStatusResponse?>
{
    public Guid OrganizerId { get; init; }

    public string Status { get; init; } = string.Empty;
}
