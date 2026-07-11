using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace OVCMOVE.Application.Features.Auth.Command.Refresh;

public class RefreshTokenCommandHandler : BaseCommandHandler<RefreshTokenCommandHandler>, IRequestHandler<RefreshTokenCommand, LoginResponse>
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

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var oldTokenEntity = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (oldTokenEntity == null || oldTokenEntity.IsRevoked)
        {
            throw new UnauthorizedAccessException("Refresh Token không tồn tại hoặc đã bị thu hồi.");
        }

        if (oldTokenEntity.ExpiryDate < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        var user = await _userRepository.GetByIdAsync(oldTokenEntity.UserId);
        
        if (user == null || user.Status == Domain.Enums.UserStatus.Deactive)
        {
            throw new UnauthorizedAccessException("Tài khoản người dùng không hợp lệ hoặc đã bị khóa.");
        }

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshTokenEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = newRefreshTokenString,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity);

        await _refreshTokenRepository.RevokeAsync(oldTokenEntity.Id);

        return new LoginResponse(newAccessToken, newRefreshTokenString, user.Id);
    }
}