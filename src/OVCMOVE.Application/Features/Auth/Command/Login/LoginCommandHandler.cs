using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Auth;

namespace OVCMOVE.Application.Features.Auth.Command.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultModel>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserAccessRepository _userAccessRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthSessionIssuer _sessionIssuer;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUserAccessRepository userAccessRepository,
        IPasswordHasher passwordHasher,
        AuthSessionIssuer sessionIssuer)
    {
        _userRepository = userRepository;
        _userAccessRepository = userAccessRepository;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
    }

    /// <summary>Authenticates a user with a username and hashed password.</summary>
    public async Task<LoginResultModel> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ApplicationValidationException(
                "Tên đăng nhập và mật khẩu không được để trống.");
        }

        var user = await _userRepository.GetByUsernameAsync(
            request.Username.Trim(),
            cancellationToken);

        if (user?.PasswordHash is null ||
            !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        var accessProfile = await _userAccessRepository.GetAccessProfileAsync(
            user.Id,
            cancellationToken);
        if (accessProfile.Roles.Count == 0)
        {
            throw new UnauthorizedAccessException(
                "Tài khoản chưa được gán role truy cập.");
        }

        return await _sessionIssuer.IssueAsync(
            user,
            accessProfile,
            cancellationToken);
    }
}
