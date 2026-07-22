using MediatR;
using Microsoft.Extensions.Logging;
using AutoMapper;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Auth.Command.GoogleLogin;

public class GoogleLoginCommandHandler : BaseCommandHandler<GoogleLoginCommandHandler>, IRequestHandler<GoogleLoginCommand, LoginResultModel>
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

    public async Task<LoginResultModel> Handle(GoogleLoginCommand request, CancellationToken cancellationToken) 
    {
        try
        {
            var googleUser = await _googleAuthService.ValidateGoogleTokenAsync(request.IdToken);
            
            if (googleUser is null || string.IsNullOrWhiteSpace(googleUser.Email))
                throw new UnauthorizedAccessException("Xác thực Google thất bại hoặc Token đã hết hạn.");

            var user = await _userRepository.GetByEmailAsync(googleUser.Email, cancellationToken);

            if (user == null || user.Role != UserConstant.Role.Organizer)
                throw new UnauthorizedAccessException("Email này chưa được cấp quyền Organizer."); 

            if (string.IsNullOrWhiteSpace(user.DisplayName) && !string.IsNullOrWhiteSpace(googleUser.DisplayName))
            {
                var displayName = googleUser.DisplayName.Trim();
                await _userRepository.UpdateDisplayNameAsync(user.Id, displayName, cancellationToken);
                user.DisplayName = displayName;
            }

            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
            var now = DateTime.UtcNow;
            var sessionId = Guid.NewGuid();

            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SessionId = sessionId,
                FamilyId = sessionId,
                TokenHash = _jwtTokenGenerator.HashRefreshToken(refreshToken),
                ExpiryDate = now.AddDays(_jwtTokenGenerator.RefreshTokenExpirationDays),
                IsRevoked = false,
                CreatedAt = now
            };
            
            await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity, cancellationToken);

            var expirationDate = DateTime.UtcNow.AddMinutes(_jwtTokenGenerator.AccessTokenExpirationMinutes);

            return new LoginResultModel
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = expirationDate,
                UserId = user.Id
            };
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not OperationCanceledException)
        {
            _logger.LogError(ex, $"Lỗi hệ thống khi xử lý: {ex.Message}."); //TODO: VIẾT LẠI LOG RIÊNG ĐỂ BIẾT CỤ THỂ LUÔN
            throw;
        }
    }
}
