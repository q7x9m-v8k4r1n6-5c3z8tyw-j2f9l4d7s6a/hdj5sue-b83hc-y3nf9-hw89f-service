using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common; 

namespace OVCMOVE.Application.Features.Auth.Command.Logout;

public class LogoutCommandHandler : BaseCommandHandler<LogoutCommandHandler>, IRequestHandler<LogoutCommand, bool>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper mapper,
        ILogger<LogoutCommandHandler> logger) : base(logger, mapper) 
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try 
        {
            var tokenHash = _jwtTokenGenerator.HashRefreshToken(request.RefreshToken);
            var tokenEntity = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (tokenEntity == null || tokenEntity.IsRevoked)
            {
                _logger.LogWarning("Logout received for an unknown or previously revoked refresh token.");
                
                return true; 
            }

            var isSuccess = await _refreshTokenRepository.RevokeAsync(tokenEntity.Id, cancellationToken);
            
            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi xử lý logout.");
            throw; 
        }
    }
}
