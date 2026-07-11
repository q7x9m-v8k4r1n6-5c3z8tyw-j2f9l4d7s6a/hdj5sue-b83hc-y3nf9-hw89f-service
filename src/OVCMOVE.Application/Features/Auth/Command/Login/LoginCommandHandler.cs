using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Enums;

namespace OVCMOVE.Application.Features.Auth.Command.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository, 
        IRefreshTokenRepository refreshTokenRepository, 
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        
        if (user == null)
        {
            throw new UnauthorizedAccessException("Tên đăng nhập không tồn tại.");
        }

        // * HÀM NÀY LÀ TẠM THỜI VÀ CHƯA CÓ CƠ CHẾ HASH ĐỂ DỄ TESTING (thêm sau khi backlog create account của Hiếu Lê xong)
        if (user.PasswordHash != request.Password)
        {
            throw new UnauthorizedAccessException("Mật khẩu không đúng.");
        }

        if (user.Status == UserStatus.Deactive)
        {
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiryDate = DateTime.UtcNow.AddDays(7), 
            IsRevoked = false
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return new LoginResponse(accessToken, refreshTokenString, user.Id);
    }
}