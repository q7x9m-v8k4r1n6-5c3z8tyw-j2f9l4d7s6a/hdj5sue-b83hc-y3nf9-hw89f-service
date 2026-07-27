using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Auth;

/// <summary>
/// Creates the access token and persists the initial refresh-token session
/// shared by password and Google sign-in flows.
/// </summary>
public class AuthSessionIssuer
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthSessionIssuer(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    /// <summary>Issues a new token pair for an authenticated user.</summary>
    public async Task<LoginResultModel> IssueAsync(
        User user,
        UserAccessProfileModel accessProfile,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiration = now.AddDays(
            _jwtTokenGenerator.RefreshTokenExpirationDays);

        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionId = sessionId,
            FamilyId = sessionId,
            TokenHash = _jwtTokenGenerator.HashRefreshToken(refreshToken),
            ExpiryDate = refreshTokenExpiration,
            IsRevoked = false,
            CreatedAt = now
        }, cancellationToken);

        return new LoginResultModel
        {
            AccessToken = _jwtTokenGenerator.GenerateAccessToken(
                user,
                accessProfile),
            AccessTokenExpiration = now.AddMinutes(
                _jwtTokenGenerator.AccessTokenExpirationMinutes),
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshTokenExpiration,
            UserId = user.Id,
            Roles = accessProfile.Roles,
            Permissions = accessProfile.Permissions,
            Access = accessProfile.Access
        };
    }
}
