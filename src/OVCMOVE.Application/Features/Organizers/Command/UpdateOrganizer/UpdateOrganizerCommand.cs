using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Organizers.Command.UpdateOrganizer;
public sealed class UpdateOrganizerCommand : AuditedRequest, IRequest<bool>
{
    public Guid OrganizerId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];
    public string Status { get; init; } = string.Empty;
}
