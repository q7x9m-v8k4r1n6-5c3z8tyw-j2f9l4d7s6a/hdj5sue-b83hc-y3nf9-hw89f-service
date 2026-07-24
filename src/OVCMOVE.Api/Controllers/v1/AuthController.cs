using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using OVCMOVE.Domain.Constants;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Application.Features.Auth.Command.Logout;
using OVCMOVE.Application.Features.Auth.Command.Refresh;
using OVCMOVE.Application.Features.Auth.Command.GoogleLogin;
using OVCMOVE.Application.Features.Auth.Queries.GetMe;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Api.Controllers.v1;

public class AuthController : BaseController<AuthController>
{
    private const string ProductionRefreshTokenCookieName = "__Host-refreshToken";
    private const string LegacyRefreshTokenCookieName = "refreshToken";
    private readonly JwtConfigOptions _jwtOptions;
    private string RefreshTokenCookieName => ProductionRefreshTokenCookieName;

    public AuthController(
        ILogger<AuthController> logger,
        IMediator mediator,
        IMapper mapper,
        IOptions<JwtConfigOptions> jwtOptions)
        : base(logger, mediator, mapper)
    {
        _jwtOptions = jwtOptions.Value;
    }


    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            throw new UnauthorizedAccessException("Token không hợp lệ.");

        var query = new GetMeQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        var response = _mapper.Map<AuthContract.MeResponse>(result);

        return Ok(new ApiResponseModel<AuthContract.MeResponse>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = response
        });

    }

    // POST host:port/api/v1/Auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthContract.LoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var command = _mapper.Map<LoginCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);

        var response = _mapper.Map<AuthContract.LoginResponse>(result);

        return Ok(new ApiResponseModel<AuthContract.LoginResponse>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = response
        });
    }

    //POST host:port/api/v1/Auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? Request.Cookies[LegacyRefreshTokenCookieName];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            // Truyền thẳng token vào Command thay vì dùng Mapper
            var command = new LogoutCommand(refreshToken);
            await _mediator.Send(command, cancellationToken);
        }

        // Delete the refresh token cookies from the client
        var options = new CookieOptions
        {
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.Lax
        };

        Response.Cookies.Delete(ProductionRefreshTokenCookieName, options);
        Response.Cookies.Delete(LegacyRefreshTokenCookieName, options);
        Response.Cookies.Delete(LegacyRefreshTokenCookieName);


        return Ok(new ApiResponseModel<bool>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = true
        });
    }

    //POST host:port/api/v1/Auth/refresh-token
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? Request.Cookies[LegacyRefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedAccessException("Không tìm thấy Refresh Token trong Cookie. Vui lòng đăng nhập lại.");

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);

        var response = _mapper.Map<AuthContract.LoginResponse>(result);

        return Ok(new ApiResponseModel<AuthContract.LoginResponse>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = response
        });
    }

    // POST host:port/api/v1/Auth/google-login
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] AuthContract.GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var command = _mapper.Map<GoogleLoginCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken);

        var response = _mapper.Map<AuthContract.LoginResponse>(result);

        return Ok(new ApiResponseModel<AuthContract.LoginResponse>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = response
        });
    }

    // ==========================================
    // FUNCTION XỬ LÝ COOKIE (PRIVATE HELPER)
    // ==========================================
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),

            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(_jwtOptions.RefreshTokenExpirationDays)
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }
}
