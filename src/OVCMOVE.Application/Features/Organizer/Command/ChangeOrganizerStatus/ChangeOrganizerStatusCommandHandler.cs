using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Organizer.Command.ChangeOrganizerStatus;

public class ChangeOrganizerStatusCommandHandler :
    BaseCommandHandler<ChangeOrganizerStatusCommandHandler>,
    IRequestHandler<ChangeOrganizerStatusCommand, OrganizerResultModel.ChangeOrganizerStatusResultModel>
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

    public async Task<OrganizerResultModel.ChangeOrganizerStatusResultModel> Handle(
        ChangeOrganizerStatusCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _organizerRepository.ChangeStatusAsync(
                request.OrganizerId,
                request.IsActive,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while handling ChangeOrganizerStatusCommand: {Message}", ex.Message);
            throw;
        }
    }
}
