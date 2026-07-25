using System.Net;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Team;
using OVCMOVE.Application.Helpers;
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
        IEmailService emailService,
        IUnitOfWork unitOfWork) : base(logger, mapper, unitOfWork)
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

            var displayName = request.DisplayName?.Trim() ?? string.Empty;
            var username = request.Username?.Trim() ?? string.Empty;
            var leaderEmail = request.LeaderEmail?.Trim() ?? string.Empty;

            if (await _userRepository.GetByUsernameAnyStatusAsync(username, cancellationToken) is not null)
            {
                throw new InvalidOperationException("Team username da duoc dang ky.");
            }

            if (await _userRepository.GetByEmailAnyStatusAsync(leaderEmail, cancellationToken) is not null)
            {
                throw new InvalidOperationException("Leader email da duoc dang ky.");
            }

            var generatedPassword = PasswordHelper.Generate();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = PasswordHelper.Hash(generatedPassword),
                Email = leaderEmail,
                Role = UserConstant.Role.Team,
                DisplayName = displayName,
                Status = UserConstant.Status.Active
            };

            var team = new Team
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TotalScore = 0,
                CreatedAt = VietnamTimeHelper.Now
            };

            var unitOfWork = _unitOfWork
                ?? throw new InvalidOperationException("Unit of work is not configured.");

            unitOfWork.Begin();
            try
            {
                await _userRepository.AddAsync(user, cancellationToken);
                await _teamRepository.AddAsync(team, cancellationToken);
                unitOfWork.Commit();
            }
            catch
            {
                unitOfWork.Rollback();
                throw;
            }

            await TrySendTeamCreatedEmailAsync(displayName, username, leaderEmail, generatedPassword, cancellationToken);

            return new TeamResponse
            {
                Id = team.Id,
                UserId = user.Id,
                Name = displayName,
                LeaderEmail = leaderEmail,
                Username = username,
                Status = user.Status
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error occurred while handling CreateTeamCommand for {LeaderEmail}.", request.LeaderEmail);
            throw;
        }
    }

    private async Task TrySendTeamCreatedEmailAsync(
        string displayName,
        string username,
        string leaderEmail,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subject = "OVCMOVE team account created";
            var body = $@"
                <p>Your OVCMOVE team account has been created.</p>
                <p><strong>Team:</strong> {WebUtility.HtmlEncode(displayName)}</p>
                <p><strong>Username:</strong> {WebUtility.HtmlEncode(username)}</p>
                <p><strong>Password:</strong> {WebUtility.HtmlEncode(password)}</p>
                <p>Account chi duoc dang nhap tren 1 may.</p>";

            await _emailService.SendAsync(leaderEmail, subject, body, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Team account was created but email could not be sent to {LeaderEmail}.", leaderEmail);
        }
    }
}
