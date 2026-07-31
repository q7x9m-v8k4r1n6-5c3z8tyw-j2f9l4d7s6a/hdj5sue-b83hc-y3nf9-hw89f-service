using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Teams.Command.UpdateTeam;

public sealed class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand, bool>
{
    private readonly ITeamRepository _teams;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<UpdateTeamCommandHandler> _logger;

    public UpdateTeamCommandHandler(
        ITeamRepository teams,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        ILogger<UpdateTeamCommandHandler> logger)
    {
        _teams = teams;
        _users = users;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        UpdateTeamCommand request,
        CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 255)
        {
            throw new ApplicationValidationException(
                "Tên đội chơi là bắt buộc và không được vượt quá 255 ký tự.");
        }
        var username = request.Username.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-z0-9-]{1,255}$"))
        {
            throw new ApplicationValidationException(
                "Tên đăng nhập đội chơi chỉ gồm chữ thường, số hoặc dấu gạch nối.");
        }
        var email = NormalizeEmail(request.Email);
        if (request.ResetPassword && string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ApplicationValidationException("Mật khẩu mới không được để trống.");
        }

        if (request.Status is not (UserConstants.Status.Active or UserConstants.Status.Inactive))
        {
            throw new ApplicationValidationException("Trạng thái đội chơi không hợp lệ.");
        }

        var team = await _teams.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null) return false;

        var usernameOwner = await _users.GetByUsernameAnyStatusAsync(
            username,
            cancellationToken);
        if (usernameOwner is not null && usernameOwner.Id != team.Id)
        {
            throw new ApplicationConflictException("Tên đăng nhập đã được sử dụng.");
        }
        var emailOwner = await _users.GetByEmailAnyStatusAsync(email, cancellationToken);
        if (emailOwner is not null && emailOwner.Id != team.Id)
        {
            throw new ApplicationConflictException("Email đã được sử dụng.");
        }

        var changes = new List<string>();
        if (!string.Equals(team.DisplayName, displayName, StringComparison.Ordinal)) changes.Add("tên đội chơi");
        if (!string.Equals(team.Username, username, StringComparison.Ordinal)) changes.Add("tên đăng nhập");
        if (!string.Equals(team.LinkedEmail, email, StringComparison.OrdinalIgnoreCase)) changes.Add("email đội trưởng");
        if (!string.Equals(team.Status, request.Status, StringComparison.Ordinal)) changes.Add("trạng thái");
        if (request.ResetPassword) changes.Add("mật khẩu");

        team.DisplayName = displayName;
        team.Username = username;
        team.LinkedEmail = email;
        if (request.ResetPassword)
        {
            team.PasswordHash = _passwordHasher.Hash(request.Password);
        }
        team.Status = request.Status;
        team.ModifiedBy = request.GetActorOrSystem();
        team.ModifiedAt = DateTime.UtcNow;
        var updated = await _teams.UpdateAsync(team, cancellationToken);
        if (updated && changes.Count > 0)
        {
            await TrySendTeamUpdatedEmailAsync(
                team,
                request.ResetPassword ? request.Password : null,
                changes,
                cancellationToken);
        }
        return updated;
    }

    private static string NormalizeEmail(string email)
    {
        try
        {
            var normalized = email.Trim().ToLowerInvariant();
            return new System.Net.Mail.MailAddress(normalized).Address == normalized
                ? normalized
                : throw new ApplicationValidationException("Email không đúng định dạng.");
        }
        catch (FormatException)
        {
            throw new ApplicationValidationException("Email không đúng định dạng.");
        }
    }

    private async Task TrySendTeamUpdatedEmailAsync(
        Domain.Entities.User team,
        string? newPassword,
        IReadOnlyCollection<string> changes,
        CancellationToken cancellationToken)
    {
        try
        {
            var details = new List<(string Label, string Value)>
            {
                ("Tên đăng nhập", team.Username ?? string.Empty),
                ("Email đội trưởng", team.LinkedEmail),
                ("Nội dung cập nhật", string.Join(", ", changes)),
            };
            if (newPassword is not null) details.Add(("Mật khẩu mới", newPassword));
            var body = AccountEmailTemplate.Build(
                "Thông tin đội chơi đã được cập nhật",
                team.DisplayName ?? team.Username ?? "đội chơi",
                "thông tin tài khoản MOVE của đội vừa được cập nhật.",
                details);
            await _emailService.SendTeamCredentialsAsync(
                team.LinkedEmail,
                AccountEmailTemplate.Subject("Thông tin đội chơi MOVE đã được cập nhật"),
                body,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "Team account was updated, but email could not be sent to {Email}.",
                team.LinkedEmail);
        }
    }
}
