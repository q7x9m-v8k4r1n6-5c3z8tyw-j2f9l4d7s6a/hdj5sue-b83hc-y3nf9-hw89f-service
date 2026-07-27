using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Application.Features.Auth.Command.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    /// <summary>Revokes the refresh token for the current login session.</summary>
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenGenerator.HashRefreshToken(request.RefreshToken);
        var tokenEntity = await _refreshTokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (tokenEntity == null || tokenEntity.IsRevoked)
        {
            _logger.LogWarning(
                "Logout received for an unknown or previously revoked refresh token.");
            return true;
        }

        return await _refreshTokenRepository.RevokeAsync(
            tokenEntity.Id,
            cancellationToken);
    }
}
