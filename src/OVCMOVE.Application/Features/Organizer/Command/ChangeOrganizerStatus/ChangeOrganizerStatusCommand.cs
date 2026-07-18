using MediatR;

namespace OVCMOVE.Application.Features.Organizer.Command.ChangeOrganizerStatus;

public class ChangeOrganizerStatusCommand : IRequest<bool>
{
    public Guid OrganizerId { get; init; }

    public string Status { get; init; } = string.Empty;
}
