using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Organizers.Command.DeleteOrganizer;

public sealed class DeleteOrganizerCommand : AuditedRequest, IRequest<bool>
{
    public Guid OrganizerId { get; init; }
}
