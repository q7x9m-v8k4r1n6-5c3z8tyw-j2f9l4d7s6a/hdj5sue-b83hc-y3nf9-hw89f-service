using System.Net;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Team;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Teams.Command.ResetTeamPassword;

public sealed class ResetTeamPasswordCommandHandler :
    BaseCommandHandler<ResetTeamPasswordCommandHandler>,
    IRequestHandler<ResetTeamPasswordCommand, TeamResponse?>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordGenerator _passwordGenerator;
    private readonly IEmailService _emailService;

    public ResetTeamPasswordCommandHandler(
        ILogger<ResetTeamPasswordCommandHandler> logger,
        IMapper mapper,
        ITeamRepository teamRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordGenerator passwordGenerator,
        IEmailService emailService) : base(logger, mapper)
    {
        _teamRepository = teamRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordGenerator = passwordGenerator;
        _emailService = emailService;
    }

    public async Task<TeamResponse?> Handle(
        ResetTeamPasswordCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
            if (team is null)
            {
                return null;
            }

            var user = await _userRepository.GetByIdAsync(team.UserId, cancellationToken);
            if (user is null || user.Role != UserConstant.Role.Team)
            {
                return null;
            }

            var generatedPassword = _passwordGenerator.Generate();
            var passwordHash = _passwordHasher.HashPassword(generatedPassword);

            await _userRepository.UpdateTeamPasswordAsync(
                user.Id,
                passwordHash,
                cancellationToken);

            await TrySendResetPasswordEmailAsync(team, generatedPassword, cancellationToken);

            return _mapper.Map<TeamResponse>(team);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while resetting password for team {TeamId}.", request.TeamId);
            throw;
        }
    }

    private async Task TrySendResetPasswordEmailAsync(
        Domain.Entities.Team team,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subject = "OVCMOVE team password reset";
            var body = $@"
                <p>Your OVCMOVE team password has been reset.</p>
                <p><strong>Team:</strong> {WebUtility.HtmlEncode(team.Name)}</p>
                <p><strong>Username:</strong> {WebUtility.HtmlEncode(team.Username)}</p>
                <p><strong>New password:</strong> {WebUtility.HtmlEncode(password)}</p>
                <p>Please change your password after signing in.</p>
                <p>Account chi duoc dang nhap tren 1 may.</p>";

            await _emailService.SendTeamCredentialsAsync(
                team.LeaderEmail,
                subject,
                body,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Team password was reset but email could not be sent to {LeaderEmail}.",
                team.LeaderEmail);
        }
    }
}
