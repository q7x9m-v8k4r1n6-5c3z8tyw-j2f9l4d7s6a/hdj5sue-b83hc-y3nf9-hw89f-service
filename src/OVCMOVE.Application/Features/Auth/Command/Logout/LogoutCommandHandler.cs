using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common; 

namespace OVCMOVE.Application.Features.Auth.Command.Logout;

public class LogoutCommandHandler : BaseCommandHandler<LogoutCommandHandler>, IRequestHandler<LogoutCommand, bool>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IMapper mapper,
        ILogger<LogoutCommandHandler> logger) : base(logger, mapper) 
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try 
        {
            var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

            if (tokenEntity == null || tokenEntity.IsRevoked)
            {
                string userIdStr = tokenEntity != null ? tokenEntity.UserId.ToString() : "Không xác định (Token giả mạo/Không tồn tại)";
                
                _logger.LogWarning("⚠️ Cảnh báo: Có hành vi Logout bất thường! UserId: {UserId}, Token: {Token}", userIdStr, request.RefreshToken);
                
                return true; 
            }

            var isSuccess = await _refreshTokenRepository.RevokeAsync(tokenEntity.Id, cancellationToken);
            
            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi xử lý LogoutCommand cho Token: {Token}", request.RefreshToken);
            throw; 
        }
    }
}