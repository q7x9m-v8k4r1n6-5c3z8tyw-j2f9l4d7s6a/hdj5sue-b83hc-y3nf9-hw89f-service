using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public sealed class CreateTeamCommandHandler :
    IRequestHandler<CreateTeamCommand, CreateTeamResponse>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUserRoleRepository _userRoles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTeamCommandHandler> _logger;

    public CreateTeamCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IUserRoleRepository userRoles,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<CreateTeamCommandHandler> logger)
    {
        _users = users;
        _roles = roles;
        _userRoles = userRoles;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateTeamResponse> Handle(
        CreateTeamCommand request,
        CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 255)
        {
            throw new ApplicationValidationException(
                "Tên đội chơi là bắt buộc và không được vượt quá 255 ký tự.");
        }

        var email = NormalizeEmail(request.Email);
        if (await _users.GetByEmailAnyStatusAsync(email, cancellationToken) is not null)
        {
            throw new ApplicationConflictException("Email đã được đăng ký.");
        }

        var teamRole = await _roles.GetByCodeAsync(
            UserConstants.RoleCode.Team,
            cancellationToken) ?? throw new ApplicationNotFoundException(
            "Không tìm thấy role team.");
        var username = await TeamUsernameHelper.GenerateUniqueAsync(
            displayName,
            _users,
            cancellationToken);
        var password = GeneratePassword();
        var now = DateTime.UtcNow;
        var actor = request.GetActorOrSystem();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = _passwordHasher.Hash(password),
            LinkedEmail = email,
            UserType = UserConstants.UserType.Team,
            DisplayName = displayName,
            ShortName = username,
            Status = UserConstants.Status.Active,
            CreatedBy = actor,
            CreatedAt = now,
            ModifiedBy = actor,
            ModifiedAt = now,
            IsDeleted = false,
        };

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await _users.AddAsync(user, cancellationToken);
            await _userRoles.CreateAsync(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = teamRole.Id,
                CreatedBy = actor,
                CreatedAt = now,
                ModifiedBy = actor,
                ModifiedAt = now,
                IsDeleted = false,
            }, cancellationToken);
            await _unitOfWork.CommitAsync(CancellationToken.None);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        await TrySendTeamCreatedEmailAsync(
            user,
            password,
            cancellationToken);
        return new CreateTeamResponse { Id = user.Id, Username = username };
    }

    private async Task TrySendTeamCreatedEmailAsync(
        User user,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = AccountEmailTemplate.Subject("Tài khoản đội chơi MOVE đã được tạo");
            var body = AccountEmailTemplate.Build(
                "Tài khoản đội chơi đã sẵn sàng",
                user.DisplayName ?? user.Username ?? "đội chơi",
                "tài khoản MOVE của đội đã được tạo thành công.",
                [("Tên đăng nhập", user.Username ?? string.Empty), ("Mật khẩu", password)]);

            await _emailService.SendTeamCredentialsAsync(
                user.LinkedEmail,
                subject,
                body,
                cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogWarning(
                exception,
                "Team account was created, but its notification email was canceled for {Email}.",
                user.LinkedEmail);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Team account was created, but email could not be sent to {Email}.",
                user.LinkedEmail);
        }
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        try
        {
            return new MailAddress(normalized).Address == normalized
                ? normalized
                : throw new ApplicationValidationException("Email không đúng định dạng.");
        }
        catch (FormatException)
        {
            throw new ApplicationValidationException("Email không đúng định dạng.");
        }
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
        {
            password[index] = characters[RandomNumberGenerator.GetInt32(characters.Length)];
        }

        for (var index = password.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[swapIndex]) = (password[swapIndex], password[index]);
        }

        return new string(password);
    }

    private static string BuildCreatedEmail(User user, string password)
    {
        string Encode(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
        return $"""
            <!doctype html>
<html lang="vi">

<body style="margin:0;padding:0;background:#f7f7f7;font-family:Arial,sans-serif;color:#1a1c1c;">
    <table role="presentation" width="100%" cellspacing="0" cellpadding="0"
        style="padding:32px 16px;background:#f7f7f7;">
        <tr>
            <td align="center">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0"
                    style="max-width:600px;background:#fff;border:1px solid #eee;border-radius:16px;overflow:hidden;">
                    <tr>
                        <td style="padding:24px 32px;background:#420001;color:#fff;">
                            <div style="font-size:22px;font-weight:700;letter-spacing:.5px;">OISP Volunteer Club</div>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:32px;">
                            <h1 style="margin:0 0 12px;font-size:24px;line-height:32px;">Tài khoản đội chơi đã sẵn sàng
                            </h1>
                            <p style="margin:0 0 24px;font-size:15px;line-height:24px;color:#525252;">Mến chào<strong
                                    style="color:#1a1c1c;">{Encode(user.DisplayName)}</strong>, tài khoản MOVE của đội
                                đã được tạo thành công.</p>
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0"
                                style="background:#fff5f5;border:1px solid #fdcacb;border-radius:12px;">
                                <tr>
                                    <td style="padding:20px;">
                                        <div
                                            style="margin-bottom:14px;font-size:12px;font-weight:700;letter-spacing:.8px;color:#8b1f21;text-transform:uppercase;">
                                            Thông tin đăng nhập</div>
                                        <div style="margin-bottom:10px;font-size:14px;color:#525252;">Tên đăng
                                            nhập<br><strong
                                                style="font-size:16px;color:#1a1c1c;">{Encode(user.Username)}</strong>
                                        </div>
                                        <div style="font-size:14px;color:#525252;">Mật khẩu<br><strong
                                                style="font-size:16px;letter-spacing:1px;color:#1a1c1c;">{Encode(password)}</strong>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            <p style="margin:24px 0 0;font-size:14px;line-height:22px;color:#525252;">
                                Chúc bạn có những trải nghiệm tuyệt vời với MOVE!</p>
                            <br/>
                            <p>Trân trọng,</p>
                            <p>OISP Volunteer Club</p>
                        </td>
                    </tr>
                    <tr>
                        <td
                            style="padding:18px 32px;background:#fafafa;border-top:1px solid #eee;font-size:12px;line-height:18px;color:#737373;">
                            Đây là email tự động từ OISP Volunteer Club.</td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>

</html>
""";
    }
}
