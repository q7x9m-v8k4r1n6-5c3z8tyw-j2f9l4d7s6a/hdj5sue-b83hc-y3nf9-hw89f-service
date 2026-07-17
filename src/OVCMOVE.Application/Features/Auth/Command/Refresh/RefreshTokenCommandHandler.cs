using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Auth.Command.Refresh;

public class RefreshTokenCommandHandler : BaseCommandHandler<RefreshTokenCommandHandler>, IRequestHandler<RefreshTokenCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<RefreshTokenCommandHandler> logger) : base(logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var oldTokenEntity = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

            if (oldTokenEntity == null || oldTokenEntity.IsRevoked)
                throw new UnauthorizedAccessException("Refresh Token không tồn tại hoặc đã bị thu hồi.");

            if (oldTokenEntity.ExpiryDate < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");

            var user = await _userRepository.GetByIdAsync(oldTokenEntity.UserId, cancellationToken);
            
            var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshTokenEntity
            {
                UserId = user.Id,
                Token = newRefreshTokenString,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpirationDays),
                IsRevoked = false
            };
            
            await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity, cancellationToken);
            await _refreshTokenRepository.RevokeAsync(oldTokenEntity.Id, cancellationToken);

            var expirationDate = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.AccessTokenExpirationMinutes);

            return new LoginResult(newAccessToken, newRefreshTokenString, expirationDate, user.Id);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi xử lý RefreshTokenCommand cho Token: {Token}", request.RefreshToken);
            throw;
        }
    }
}