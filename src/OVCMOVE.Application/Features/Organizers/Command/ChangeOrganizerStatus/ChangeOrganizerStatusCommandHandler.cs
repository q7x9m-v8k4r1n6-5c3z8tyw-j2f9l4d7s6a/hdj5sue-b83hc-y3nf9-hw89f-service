using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Organizers.Command.ChangeOrganizerStatus;

public class ChangeOrganizerStatusCommandHandler :
    IRequestHandler<ChangeOrganizerStatusCommand, OrganizerStatusResponse?>
{
    private readonly IOrganizerRepository _organizerRepository;

    public ChangeOrganizerStatusCommandHandler(
        IOrganizerRepository organizerRepository)
    {
        _organizerRepository = organizerRepository;
    }

    /// <summary>Changes an organizer account status when the account exists.</summary>
    public async Task<OrganizerStatusResponse?> Handle(
        ChangeOrganizerStatusCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var status = (request.Status ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
        if (status is not UserConstants.Status.Active and
            not UserConstants.Status.Inactive)
        {
            throw new ApplicationValidationException(
                $"Trạng thái organizer '{request.Status}' không hợp lệ.");
        }

        var result = await _organizerRepository.ChangeStatusAsync(
            request.OrganizerId,
            status,
            cancellationToken);

        if (!result)
        {
            return null;
        }

        return new OrganizerStatusResponse
        {
            OrganizerId = request.OrganizerId,
            Status = status
        };
    }
}
