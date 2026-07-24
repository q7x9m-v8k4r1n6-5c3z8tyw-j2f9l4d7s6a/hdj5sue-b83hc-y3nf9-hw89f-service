using System.Net;
using System.Net.Mail;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Team;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Teams.Command.UpdateTeam;

public class UpdateTeamCommandHandler :
    BaseCommandHandler<UpdateTeamCommandHandler>,
    IRequestHandler<UpdateTeamCommand, TeamResponse?>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public UpdateTeamCommandHandler(
        ILogger<UpdateTeamCommandHandler> logger,
        IMapper mapper,
        ITeamRepository teamRepository,
        IUserRepository userRepository,
        IEmailService emailService) : base(logger, mapper)
    {
        _teamRepository = teamRepository;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<TeamResponse?> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var displayName = request.DisplayName?.Trim() ?? string.Empty;
            var username = request.Username?.Trim() ?? string.Empty;
            var leaderEmail = request.LeaderEmail?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(displayName) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(leaderEmail))
            {
                throw new InvalidOperationException("Team display name, username and leader email are required.");
            }

            if (!IsValidUsername(username))
            {
                throw new InvalidOperationException("Team username must be lowercase, unsigned and without spaces.");
            }

            if (!IsValidEmail(leaderEmail))
            {
                throw new InvalidOperationException("Invalid leader email format.");
            }

            var existingTeam = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
            if (existingTeam is null)
            {
                return null;
            }

            var usernameTeam = await _teamRepository.GetByUsernameAsync(username, cancellationToken);
            if (usernameTeam is not null && usernameTeam.Id != request.TeamId)
            {
                throw new InvalidOperationException("Team username da duoc dang ky.");
            }

            var usernameUser = await _userRepository.GetByUsernameAnyStatusAsync(username, cancellationToken);
            if (usernameUser is not null && usernameUser.Id != existingTeam.UserId)
            {
                throw new InvalidOperationException("Team username da duoc dang ky.");
            }

            var emailTeam = await _teamRepository.GetByLeaderEmailAsync(leaderEmail, cancellationToken);
            if (emailTeam is not null && emailTeam.Id != request.TeamId)
            {
                throw new InvalidOperationException("Leader email da duoc dang ky.");
            }

            var emailUser = await _userRepository.GetByEmailAnyStatusAsync(leaderEmail, cancellationToken);
            if (emailUser is not null && emailUser.Id != existingTeam.UserId)
            {
                throw new InvalidOperationException("Leader email da duoc dang ky.");
            }

            var updatedUser = new User
            {
                Id = existingTeam.UserId,
                Username = username,
                Email = leaderEmail,
                Role = UserConstant.Role.Team,
                DisplayName = displayName,
                ModifiedAt = DateTime.UtcNow
            };

            await _userRepository.UpdateTeamAccountAsync(updatedUser, cancellationToken);

            var updatedTeam = new Team
            {
                Id = existingTeam.Id,
                UserId = existingTeam.UserId,
                TotalScore = existingTeam.TotalScore,
                Name = displayName,
                LeaderEmail = leaderEmail,
                Username = username,
                Status = existingTeam.Status
            };

            await TrySendTeamUpdatedEmailAsync(updatedTeam, cancellationToken);

            return _mapper.Map<TeamResponse>(updatedTeam);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while handling UpdateTeamCommand for {TeamId}.", request.TeamId);
            throw;
        }
    }

    private async Task TrySendTeamUpdatedEmailAsync(Team team, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subject = "OVCMOVE team account updated";
            var body = $@"
                <p>Your OVCMOVE team account has been updated.</p>
                <p><strong>Team:</strong> {WebUtility.HtmlEncode(team.Name)}</p>
                <p><strong>Username:</strong> {WebUtility.HtmlEncode(team.Username)}</p>
                <p>Account chi duoc dang nhap tren 1 may.</p>";

            await _emailService.SendTeamCredentialsAsync(team.LeaderEmail, subject, body, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Team account was updated but email could not be sent to {LeaderEmail}.", team.LeaderEmail);
        }
    }

    private static bool IsValidUsername(string username)
    {
        return username.All(character =>
            character is >= 'a' and <= 'z' ||
            character is >= '0' and <= '9');
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
