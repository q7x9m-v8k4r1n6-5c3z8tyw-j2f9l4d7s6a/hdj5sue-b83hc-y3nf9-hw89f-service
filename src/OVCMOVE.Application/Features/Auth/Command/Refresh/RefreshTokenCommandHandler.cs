using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Auth.Command.Refresh;

public class RefreshTokenCommandHandler : BaseCommandHandler<RefreshTokenCommandHandler>, IRequestHandler<RefreshTokenCommand, LoginResultModel>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper mapper,
        ILogger<RefreshTokenCommandHandler> logger) : base(logger, mapper)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResultModel> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var oldTokenHash = _jwtTokenGenerator.HashRefreshToken(request.RefreshToken);
            var oldTokenEntity = await _refreshTokenRepository.GetByTokenHashAsync(oldTokenHash, cancellationToken);

            if (oldTokenEntity == null)
                throw new UnauthorizedAccessException("Refresh Token không tồn tại hoặc đã bị thu hồi.");

            if (oldTokenEntity.IsRevoked)
            {
                await _refreshTokenRepository.RevokeFamilyAsync(oldTokenEntity.FamilyId, DateTime.UtcNow, cancellationToken);
                _logger.LogWarning("Refresh-token reuse detected for session {SessionId}; its token family was revoked.", oldTokenEntity.SessionId);
                throw new UnauthorizedAccessException("Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
            }

            if (oldTokenEntity.ExpiryDate < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");

            var user = await _userRepository.GetByIdAsync(oldTokenEntity.UserId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Người dùng không còn tồn tại.");
            
            var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();
            var now = DateTime.UtcNow;

            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SessionId = oldTokenEntity.SessionId,
                FamilyId = oldTokenEntity.FamilyId,
                TokenHash = _jwtTokenGenerator.HashRefreshToken(newRefreshTokenString),
                ExpiryDate = now.AddDays(_jwtTokenGenerator.RefreshTokenExpirationDays),
                IsRevoked = false,
                CreatedAt = now
            };

            var rotated = await _refreshTokenRepository.TryRotateAsync(oldTokenHash, newRefreshTokenEntity, now, cancellationToken);
            if (!rotated)
            {
                // A revoked refresh token being presented again is a reuse signal.
                // Invalidate only its family, leaving other devices signed in.
                await _refreshTokenRepository.RevokeFamilyAsync(oldTokenEntity.FamilyId, now, cancellationToken);
                _logger.LogWarning("Refresh-token reuse detected for session {SessionId}; its token family was revoked.", oldTokenEntity.SessionId);
                throw new UnauthorizedAccessException("Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
            }

            var expirationDate = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.AccessTokenExpirationMinutes);

            return new LoginResultModel
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                AccessTokenExpiration = expirationDate,
                UserId = user.Id
            };
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi xử lý refresh token.");
            throw;
        }
    }
}
