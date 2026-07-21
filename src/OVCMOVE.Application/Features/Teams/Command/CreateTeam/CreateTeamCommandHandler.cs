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

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public class CreateTeamCommandHandler :
    BaseCommandHandler<CreateTeamCommandHandler>,
    IRequestHandler<CreateTeamCommand, TeamResponse>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public CreateTeamCommandHandler(
        ILogger<CreateTeamCommandHandler> logger,
        IMapper mapper,
        ITeamRepository teamRepository,
        IUserRepository userRepository,
        IEmailService emailService) : base(logger, mapper)
    {
        _teamRepository = teamRepository;
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<TeamResponse> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var teamName = request.Name.Trim();
            var leaderEmail = request.LeaderEmail.Trim();
            var username = BuildUsername(teamName);

            if (string.IsNullOrWhiteSpace(teamName) ||
                string.IsNullOrWhiteSpace(leaderEmail) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                throw new InvalidOperationException("Team name, leader email and password are required.");
            }

            if (await _teamRepository.GetByUsernameAsync(username, cancellationToken) is not null ||
                await _userRepository.GetByUsernameAnyStatusAsync(username, cancellationToken) is not null)
            {
                throw new InvalidOperationException("Team username da duoc dang ky.");
            }

            if (await _teamRepository.GetByLeaderEmailAsync(leaderEmail, cancellationToken) is not null ||
                await _userRepository.GetByEmailAnyStatusAsync(leaderEmail, cancellationToken) is not null)
            {
                throw new InvalidOperationException("Leader email da duoc dang ky.");
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = request.Password,
                Email = leaderEmail,
                Role = UserConstant.Role.Team,
                DisplayName = teamName,
                Status = UserConstant.Status.Active,
                CreatedAt = now,
                ModifiedAt = now
            };

            var team = new Team
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TotalScore = 0,
                Name = teamName,
                LeaderEmail = leaderEmail,
                Username = username,
                Status = TeamConstants.TeamStatus.Active,
                CreatedAt = now,
                ModifiedAt = now
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _teamRepository.AddAsync(team, cancellationToken);
            await TrySendTeamCreatedEmailAsync(team, request.Password, cancellationToken);

            return _mapper.Map<TeamResponse>(team);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while handling CreateTeamCommand for {LeaderEmail}.", request.LeaderEmail);
            throw;
        }
    }

    private async Task TrySendTeamCreatedEmailAsync(Team team, string password, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subject = "OVCMOVE team account created";
            var body = $@"
                <p>Your OVCMOVE team account has been created.</p>
                <p><strong>Team:</strong> {WebUtility.HtmlEncode(team.Name)}</p>
                <p><strong>Username:</strong> {WebUtility.HtmlEncode(team.Username)}</p>
                <p><strong>Password:</strong> {WebUtility.HtmlEncode(password)}</p>
                <p>This account should be signed in on one device only.</p>";

            await _emailService.SendTeamCredentialsAsync(team.LeaderEmail, subject, body, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Team account was created but email could not be sent to {LeaderEmail}.", team.LeaderEmail);
        }
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
}
