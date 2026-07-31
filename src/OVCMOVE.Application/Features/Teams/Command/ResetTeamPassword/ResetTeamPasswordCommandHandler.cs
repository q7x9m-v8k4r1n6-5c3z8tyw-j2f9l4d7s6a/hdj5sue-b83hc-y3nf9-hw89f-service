using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Command.ResetTeamPassword;

public sealed class ResetTeamPasswordCommandHandler(
    ITeamRepository teams,
    IPasswordHasher passwordHasher,
    IEmailService emailService,
    ILogger<ResetTeamPasswordCommandHandler> logger)
    : IRequestHandler<ResetTeamPasswordCommand, bool>
{
    public async Task<bool> Handle(
        ResetTeamPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null) return false;

        var password = GeneratePassword();
        team.PasswordHash = passwordHasher.Hash(password);
        team.ModifiedBy = request.GetActorOrSystem();
        team.ModifiedAt = DateTime.UtcNow;
        if (!await teams.UpdateAsync(team, cancellationToken)) return false;

        try
        {
            await emailService.SendTeamCredentialsAsync(
                team.LinkedEmail,
                AccountEmailTemplate.Subject("Mật khẩu MOVE của đội đã được cấp lại"),
                AccountEmailTemplate.Build(
                    "Mật khẩu đã được cấp lại",
                    team.DisplayName ?? team.Username ?? "đội chơi",
                    "mật khẩu tài khoản MOVE của đội đã được cấp lại.",
                    [("Tên đăng nhập", team.Username ?? string.Empty), ("Mật khẩu mới", password)]),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception,
                "Team password was reset, but email could not be sent to {Email}.",
                team.LinkedEmail);
        }

        return true;
    }

    private static string GeneratePassword()
    {
        const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        const string numbers = "23456789";
        const string characters = letters + numbers;
        Span<char> password = stackalloc char[6];
        password[0] = letters[RandomNumberGenerator.GetInt32(letters.Length)];
        password[1] = numbers[RandomNumberGenerator.GetInt32(numbers.Length)];
        for (var index = 2; index < password.Length; index++)
            password[index] = characters[RandomNumberGenerator.GetInt32(characters.Length)];
        for (var index = password.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[swapIndex]) = (password[swapIndex], password[index]);
        }
        return new string(password);
    }

}
