using MediatR;
using Microsoft.Extensions.Logging;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Enums;

namespace OVCMOVE.Application.Features.Auth.Command.GoogleLogin;

public class GoogleLoginCommandHandler : BaseCommandHandler<GoogleLoginCommandHandler>, IRequestHandler<GoogleLoginCommand, LoginResponse>
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
        ILogger<GoogleLoginCommandHandler> logger) : base(logger) 
    {
        _googleAuthService = googleAuthService;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken) 
    {
        var email = await _googleAuthService.ValidateGoogleTokenAndGetEmailAsync(request.IdToken);
        
        if (string.IsNullOrEmpty(email))
        {
            throw new UnauthorizedAccessException("Xác thực Google thất bại hoặc Token đã hết hạn.");
        }

        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Email này chưa được cấp quyền Organizer.");
        }
        if (user.Status == UserStatus.Deactive) 
        {
            throw new UnauthorizedAccessException("Tài khoản Organizer của bạn đã bị khóa");
        } 
        if (user.Role != UserRole.Organizer)
        {
            throw new UnauthorizedAccessException("Tài khoản cần phải là Organizer để đăng nhập");
        }

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
        await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity);

        return new LoginResponse(accessToken, refreshToken, user.Id);
    }
}