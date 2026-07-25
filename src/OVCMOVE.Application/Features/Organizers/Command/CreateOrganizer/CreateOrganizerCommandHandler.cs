using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OVCMOVE.Application.Features.Organizers.Command.CreateOrganizer;

public class CreateOrganizerCommandHandler : IRequestHandler<CreateOrganizerCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizerRepository _organizerRepository;
    private readonly ILogger<CreateOrganizerCommandHandler> _logger;

    public CreateOrganizerCommandHandler(
        IUserRepository userRepository, 
        IOrganizerRepository organizerRepository, 
        ILogger<CreateOrganizerCommandHandler> logger)
    {
        _userRepository = userRepository;
        _organizerRepository = organizerRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(CreateOrganizerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                DisplayName = request.DisplayName,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = request.Password, // plain text as per mock
                Role = string.IsNullOrWhiteSpace(request.Role) ? UserConstant.Role.Organizer : request.Role,
                Status = string.IsNullOrWhiteSpace(request.Status) ? UserConstant.Status.Active : request.Status,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin",
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = "admin",
                IsDeleted = false
            };

            await _userRepository.AddAsync(user, cancellationToken);

            var organizer = new OVCMOVE.Domain.Entities.Organizer
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin",
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = "admin",
                IsDeleted = false
            };

            await _organizerRepository.AddAsync(organizer, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organizer.");
            return false;
        }
    }
}
