using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Auth;

namespace OVCMOVE.Application.Features.Auth.Command.GoogleLogin;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResultModel>
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IUserRepository _userRepository;
    private readonly IUserAccessRepository _userAccessRepository;
    private readonly AuthSessionIssuer _sessionIssuer;

    public GoogleLoginCommandHandler(
        IGoogleAuthService googleAuthService,
        IUserRepository userRepository,
        IUserAccessRepository userAccessRepository,
        AuthSessionIssuer sessionIssuer)
    {
        _googleAuthService = googleAuthService;
        _userRepository = userRepository;
        _userAccessRepository = userAccessRepository;
        _sessionIssuer = sessionIssuer;
    }

    /// <summary>Authenticates an authorized user with a Google identity token.</summary>
    public async Task<LoginResultModel> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new ApplicationValidationException(
                "Google ID token không được để trống.");
        }

        var googleUser = await _googleAuthService.ValidateGoogleTokenAsync(
            request.IdToken,
            cancellationToken);
        if (googleUser is null || string.IsNullOrWhiteSpace(googleUser.Email))
        {
            throw new UnauthorizedAccessException(
                "Xác thực Google thất bại hoặc token đã hết hạn.");
        }

        var user = await _userRepository.GetByEmailAsync(
            googleUser.Email,
            cancellationToken)
            ?? throw new UnauthorizedAccessException(
                "Email này chưa được cấp quyền truy cập.");

        if (string.IsNullOrWhiteSpace(user.DisplayName) &&
            !string.IsNullOrWhiteSpace(googleUser.DisplayName))
        {
            var displayName = googleUser.DisplayName.Trim();
            if (displayName.Length > 255)
            {
                // External profile text must fit the stable Users column.
                displayName = displayName[..255];
            }

            await _userRepository.UpdateDisplayNameAsync(
                user.Id,
                displayName,
                cancellationToken);
            user.DisplayName = displayName;
        }

        var accessProfile = await _userAccessRepository.GetAccessProfileAsync(
            user.Id,
            cancellationToken);
        if (accessProfile.Roles.Count == 0)
        {
            throw new UnauthorizedAccessException(
                "Email này chưa được gán role truy cập.");
        }

        return await _sessionIssuer.IssueAsync(
            user,
            accessProfile,
            cancellationToken);
    }
}
