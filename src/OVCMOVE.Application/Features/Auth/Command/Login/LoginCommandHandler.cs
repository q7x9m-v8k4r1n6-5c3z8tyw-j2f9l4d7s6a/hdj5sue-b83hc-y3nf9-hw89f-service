using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Auth.Command.Login;

public class LoginCommandHandler : BaseCommandHandler<LoginCommandHandler>, IRequestHandler<LoginCommand, LoginResult>
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

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            
            // * HÀM so sánh pass này LÀ TẠM THỜI VÀ CHƯA CÓ CƠ CHẾ HASH ĐỂ DỄ TESTING
            if (user == null || user.PasswordHash != request.Password)
                throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
                
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshTokenEntity
            {
                UserId = user.Id,
                Token = refreshTokenString,
                ExpiryDate = DateTime.UtcNow.AddDays(7), 
                IsRevoked = false
            };

            await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

            var expirationDate = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.AccessTokenExpirationMinutes);

            return new LoginResult(accessToken, refreshTokenString, expirationDate, user.Id); 
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi xử lý LoginCommand cho Username: {Username}", request.Username);
            throw;
        }
    }
}