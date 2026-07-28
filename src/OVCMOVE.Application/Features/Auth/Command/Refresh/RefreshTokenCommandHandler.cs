using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Auth.Command.Refresh;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResultModel>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserAccessRepository _userAccessRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUserAccessRepository userAccessRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _userAccessRepository = userAccessRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    /// <summary>Rotates a valid refresh token and issues a new token pair.</summary>
    public async Task<LoginResultModel> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var oldTokenHash = _jwtTokenGenerator.HashRefreshToken(request.RefreshToken);
        var oldTokenEntity = await _refreshTokenRepository.GetByTokenHashAsync(oldTokenHash, cancellationToken);

        if (oldTokenEntity == null)
            throw new UnauthorizedAccessException("Refresh Token không tồn tại hoặc đã bị thu hồi.");

        if (oldTokenEntity.IsRevoked)
        {
            await RevokeReusedTokenFamilyAsync(oldTokenEntity, DateTime.UtcNow, cancellationToken);
        }

        if (oldTokenEntity.ExpiryDate < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");

        var user = await _userRepository.GetByIdAsync(oldTokenEntity.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Người dùng không còn tồn tại.");

        var accessProfile = await _userAccessRepository.GetAccessProfileAsync(user.Id, cancellationToken);
        if (accessProfile.Roles.Count == 0)
        {
            throw new UnauthorizedAccessException("Tài khoản chưa được gán role truy cập.");
        }

        var now = DateTime.UtcNow;
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiration = now.AddDays(
            _jwtTokenGenerator.RefreshTokenExpirationDays);
        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionId = oldTokenEntity.SessionId,
            FamilyId = oldTokenEntity.FamilyId,
            TokenHash = _jwtTokenGenerator.HashRefreshToken(newRefreshToken),
            ExpiryDate = refreshTokenExpiration,
            IsRevoked = false,
            CreatedAt = now
        };

        var rotated = await _refreshTokenRepository.TryRotateAsync(
            oldTokenHash,
            newRefreshTokenEntity,
            now,
            cancellationToken);
        if (!rotated)
        {
            await RevokeReusedTokenFamilyAsync(
                oldTokenEntity,
                now,
                cancellationToken);
        }

        return new LoginResultModel
        {
            AccessToken = _jwtTokenGenerator.GenerateAccessToken(user, accessProfile),
            RefreshToken = newRefreshToken,
            AccessTokenExpiration = now.AddMinutes(
                _jwtTokenGenerator.AccessTokenExpirationMinutes),
            RefreshTokenExpiration = refreshTokenExpiration,
            UserId = user.Id,
            UserType = user.UserType,
            Roles = accessProfile.Roles,
            Permissions = accessProfile.Permissions,
            Access = accessProfile.Access
        };
    }

    /// <summary>Revokes one login session after detecting refresh-token reuse.</summary>
    private async Task RevokeReusedTokenFamilyAsync(
        RefreshToken token,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await _refreshTokenRepository.RevokeFamilyAsync(
            token.FamilyId,
            utcNow,
            cancellationToken);
        _logger.LogWarning(
            "Refresh-token reuse detected for session {SessionId}; its token family was revoked.",
            token.SessionId);
        throw new UnauthorizedAccessException(
            "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
    }
}
