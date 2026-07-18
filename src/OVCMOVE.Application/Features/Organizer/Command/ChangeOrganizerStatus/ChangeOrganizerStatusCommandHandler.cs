using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Organizer.Command.ChangeOrganizerStatus;

public class ChangeOrganizerStatusCommandHandler :
    BaseCommandHandler<ChangeOrganizerStatusCommandHandler>,
    IRequestHandler<ChangeOrganizerStatusCommand, bool>
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

    public async Task<bool> Handle(
        ChangeOrganizerStatusCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _organizerRepository.ChangeStatusAsync(
                request.OrganizerId,
                request.Status,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling ChangeOrganizerStatusCommand.");
            throw;
        }
    }
}
