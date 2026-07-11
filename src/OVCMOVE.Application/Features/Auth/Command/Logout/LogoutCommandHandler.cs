using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common; 

namespace OVCMOVE.Application.Features.Auth.Command.Logout;

public class LogoutCommandHandler : BaseCommandHandler<LogoutCommandHandler>, IRequestHandler<LogoutCommand, bool>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutCommandHandler> logger) : base(logger) 
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (tokenEntity == null || tokenEntity.IsRevoked)
        {
            _logger.LogWarning("⚠️ Cảnh báo: ai đó đã Logout bằng một Token không tồn tại hoặc đã bị hủy! Token: {Token}", request.RefreshToken);
            return true; 
        }

        var isSuccess = await _refreshTokenRepository.RevokeAsync(tokenEntity.Id);
        
        return isSuccess;
    }
}