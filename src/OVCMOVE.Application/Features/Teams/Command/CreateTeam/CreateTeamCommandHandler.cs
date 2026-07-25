using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreateTeamCommandHandler> _logger;

    public CreateTeamCommandHandler(IUserRepository userRepository, ILogger<CreateTeamCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                DisplayName = request.DisplayName,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = request.Password, // plain text as per mock
                Role = UserConstant.Role.Team,
                Status = string.IsNullOrWhiteSpace(request.Status) ? UserConstant.Status.Active : request.Status,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin",
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = "admin",
                IsDeleted = false
            };

            await _userRepository.AddAsync(user, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team.");
            return false;
        }
    }
}
