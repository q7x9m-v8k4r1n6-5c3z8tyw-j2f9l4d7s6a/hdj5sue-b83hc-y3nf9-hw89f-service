using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Auth.Command.Login;

public class LoginCommandHandler : BaseCommandHandler<LoginCommandHandler>, IRequestHandler<LoginCommand, LoginResultModel>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository, 
        IRefreshTokenRepository refreshTokenRepository, 
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper mapper,
        ILogger<LoginCommandHandler> logger) : base(logger, mapper) 
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResultModel> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            
            // * HÀM so sánh pass này LÀ TẠM THỜI VÀ CHƯA CÓ CƠ CHẾ HASH ĐỂ DỄ TESTING
            if (user == null || user.PasswordHash != request.Password)
                throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
                
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();
            var now = DateTime.UtcNow;
            var sessionId = Guid.NewGuid();

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SessionId = sessionId,
                FamilyId = sessionId,
                TokenHash = _jwtTokenGenerator.HashRefreshToken(refreshTokenString),
                ExpiryDate = now.AddDays(_jwtTokenGenerator.RefreshTokenExpirationDays),
                IsRevoked = false,
                CreatedAt = now
            };

            await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

            var expirationDate = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.AccessTokenExpirationMinutes);

            return new LoginResultModel
            {
                AccessToken = accessToken,
                AccessTokenExpiration = expirationDate,
                RefreshToken = refreshTokenString,
                UserId = user.Id
            };
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi xử lý LoginCommand cho Username: {Username}", request.Username);
            throw;
        }
    }
}
