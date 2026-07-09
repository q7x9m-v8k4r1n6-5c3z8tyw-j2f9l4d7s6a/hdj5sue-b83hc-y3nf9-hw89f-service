using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Organizer;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Enums;

namespace OVCMOVE.Application.Organizers.Commands;

public class CreateOrganizerHandler : BaseCommandHandler<CreateOrganizerHandler>, IRequestHandler<CreateOrganizerCommand, OrganizerResponse>
{
    private readonly IOrganizerRepository _organizerRepo;
    private readonly IMapper _mapper;

    public CreateOrganizerHandler(
        ILogger<CreateOrganizerHandler> logger,
        IOrganizerRepository organizerRepo,
        IMapper mapper)
        : base(logger)
    {
        _organizerRepo = organizerRepo;
        _mapper = mapper;
    }

    public async Task<OrganizerResponse> Handle(CreateOrganizerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _organizerRepo.GetByEmailAsync(request.Email, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException("Email da duoc dang ky.");
            }

            var organizer = new Organizer
            {
                Id = Guid.NewGuid(),
                DisplayName = request.Email,
                Email = request.Email,
                Role = request.Role,
                Status = OrganizerStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _organizerRepo.AddAsync(organizer, cancellationToken);

            return _mapper.Map<OrganizerResponse>(organizer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling CreateOrganizerCommand for {Email}.", request.Email);
            throw;
        }
    }
}
