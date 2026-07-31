using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Organizers.Query.GetOrganizerDetail;

public sealed class GetOrganizerDetailQueryHandler(IOrganizerRepository organizers, IUserRoleRepository userRoles)
    : IRequestHandler<GetOrganizerDetailQuery, GetOrganizerDetailResult?>
{
    public async Task<GetOrganizerDetailResult?> Handle(GetOrganizerDetailQuery request, CancellationToken cancellationToken)
    {
        var organizer = await organizers.GetByIdAsync(request.OrganizerId, cancellationToken);
        if (organizer is null) return null;
        var roleIds = await userRoles.GetRoleIdsByUserIdAsync(organizer.Id, cancellationToken);
        return new GetOrganizerDetailResult
        {
            Id = organizer.Id,
            DisplayName = organizer.DisplayName ?? organizer.LinkedEmail,
            Username = organizer.Username ?? organizer.ShortName ?? string.Empty,
            Email = organizer.LinkedEmail,
            Status = organizer.Status,
            RoleIds = roleIds,
        };
    }
}
