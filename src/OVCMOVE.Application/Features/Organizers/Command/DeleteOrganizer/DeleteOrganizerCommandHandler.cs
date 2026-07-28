using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Organizers.Command.DeleteOrganizer;

public sealed class DeleteOrganizerCommandHandler(IUserRepository users)
    : IRequestHandler<DeleteOrganizerCommand, bool>
{
    public async Task<bool> Handle(DeleteOrganizerCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var organizer = await users.GetByIdAsync(request.OrganizerId, cancellationToken);
        if (organizer?.UserType != UserConstants.UserType.Organizer)
        {
            return false;
        }

        return await users.SoftDeleteAsync(
            request.OrganizerId,
            UserConstants.UserType.Organizer,
            request.GetActorOrSystem(),
            DateTime.UtcNow,
            cancellationToken);
    }
}
