using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Organizer;

namespace OVCMOVE.Application.Features.Organizer.Command.ChangeOrganizerStatus;

public class ChangeOrganizerStatusCommandHandler :
    BaseCommandHandler<ChangeOrganizerStatusCommandHandler>,
    IRequestHandler<ChangeOrganizerStatusCommand, OrganizerStatusResponse?>
{
    private readonly IOrganizerRepository _organizerRepository;

    public ChangeOrganizerStatusCommandHandler(
        ILogger<ChangeOrganizerStatusCommandHandler> logger,
        IMapper mapper,
        IOrganizerRepository organizerRepository)
        : base(logger, mapper)
    {
        _organizerRepository = organizerRepository;
    }

    public async Task<OrganizerStatusResponse?> Handle(
        ChangeOrganizerStatusCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _organizerRepository.ChangeStatusAsync(
                request.OrganizerId,
                request.Status,
                cancellationToken);

            if (!result)
            {
                return null;
            }

            return new OrganizerStatusResponse
            {
                OrganizerId = request.OrganizerId,
                Status = request.Status
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling ChangeOrganizerStatusCommand.");
            throw;
        }
    }
}
