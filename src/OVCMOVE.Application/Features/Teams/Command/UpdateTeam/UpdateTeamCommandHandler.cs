using System.Globalization;
using System.Net;
using System.Text;
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

            var teamName = request.Name.Trim();
            var leaderEmail = request.LeaderEmail.Trim();
            var password = request.Password.Trim();
            var username = BuildUsername(teamName);

            if (string.IsNullOrWhiteSpace(teamName) ||
                string.IsNullOrWhiteSpace(leaderEmail) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Team name, leader email and password are required.");
            }

            if (!IsValidUsername(teamName) || teamName != username)
            {
                throw new InvalidOperationException("Team username must be lowercase, unsigned and without spaces.");
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
                PasswordHash = password,
                Email = leaderEmail,
                Role = UserConstant.Role.Team,
                DisplayName = teamName,
                ModifiedAt = DateTime.UtcNow
            };

            await _userRepository.UpdateTeamAccountAsync(updatedUser, cancellationToken);

            var updatedTeam = new Team
            {
                Id = existingTeam.Id,
                UserId = existingTeam.UserId,
                TotalScore = existingTeam.TotalScore,
                Name = teamName,
                LeaderEmail = leaderEmail,
                Username = username,
                Status = existingTeam.Status
            };

            await SendTeamUpdatedEmailAsync(updatedTeam, password, cancellationToken);

            return _mapper.Map<TeamResponse>(updatedTeam);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while handling UpdateTeamCommand for {TeamId}.", request.TeamId);
            throw;
        }
    }

    private async Task SendTeamUpdatedEmailAsync(Team team, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var subject = "OVCMOVE team account updated";
        var body = $@"
            <p>Your OVCMOVE team account has been updated.</p>
            <p><strong>Team:</strong> {WebUtility.HtmlEncode(team.Name)}</p>
            <p><strong>Username:</strong> {WebUtility.HtmlEncode(team.Username)}</p>
            <p><strong>Password:</strong> {WebUtility.HtmlEncode(password)}</p>
            <p>Account chi duoc dang nhap tren 1 may.</p>";

        await _emailService.SendTeamCredentialsAsync(team.LeaderEmail, subject, body, cancellationToken);
    }

    private static string BuildUsername(string teamName)
    {
        var normalized = teamName.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (character is 'đ' or 'Đ')
            {
                builder.Append('d');
                continue;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark || char.IsWhiteSpace(character))
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsValidUsername(string username)
    {
        return username.All(character =>
            character is >= 'a' and <= 'z' ||
            character is >= '0' and <= '9');
    }
}
