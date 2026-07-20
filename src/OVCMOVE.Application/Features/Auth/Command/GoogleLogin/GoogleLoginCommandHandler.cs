using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Auth.Command.GoogleLogin;

public class GoogleLoginCommandHandler : BaseCommandHandler<GoogleLoginCommandHandler>, IRequestHandler<GoogleLoginCommand, LoginResult>
{
    private readonly IGoogleAuthService _googleAuthService; 
    private readonly IUserRepository _userRepository;       
    private readonly IRefreshTokenRepository _refreshTokenRepository; 
    private readonly IJwtTokenGenerator _jwtTokenGenerator; 

    public GoogleLoginCommandHandler(
        IGoogleAuthService googleAuthService,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IMapper mapper,
        ILogger<GoogleLoginCommandHandler> logger) : base(logger,mapper) 
    {
        _googleAuthService = googleAuthService;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> Handle(GoogleLoginCommand request, CancellationToken cancellationToken) 
    {
        try
        {
            var email = await _googleAuthService.ValidateGoogleTokenAndGetEmailAsync(request.IdToken);
            
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("Xác thực Google thất bại hoặc Token đã hết hạn.");

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user == null || user.Role != UserConstant.Role.Organizer)
                throw new UnauthorizedAccessException("Email này chưa được cấp quyền Organizer."); 

            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpirationDays), 
                IsRevoked = false
            };
            
            await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity, cancellationToken);

            var expirationDate = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.AccessTokenExpirationMinutes);

            return new LoginResult(accessToken, refreshToken, expirationDate, user.Id);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, $"Lỗi hệ thống khi xử lý: {ex.Message}."); //TODO: VIẾT LẠI LOG RIÊNG ĐỂ BIẾT CỤ THỂ LUÔN
            throw;
        }
    }
}