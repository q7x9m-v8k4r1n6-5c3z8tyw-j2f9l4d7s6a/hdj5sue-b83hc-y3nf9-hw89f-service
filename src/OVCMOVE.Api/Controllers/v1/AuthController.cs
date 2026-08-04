using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

using OVCMOVE.Api.Security;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Application.Features.Auth.Command.Logout;
using OVCMOVE.Application.Features.Auth.Command.Refresh;
using OVCMOVE.Application.Features.Auth.Command.GoogleLogin;
using OVCMOVE.Application.Features.Auth.Query.GetMe;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Services.LoginLockoutService;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class AuthController : BaseController
{
    private const string ProductionRefreshTokenCookieName = "__Host-refreshToken";
    private const string LegacyRefreshTokenCookieName = "refreshToken";
    private string RefreshTokenCookieName => ProductionRefreshTokenCookieName;
    private readonly ILoginLockoutService _lockoutService; // Inject service

    public AuthController(IMediator mediator, ILoginLockoutService lockoutService) : base(mediator)
    {
        _lockoutService = lockoutService;
    }

    [HttpGet("me")]
    [RequirePermission(PermissionCodes.AuthProfileRead)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) ||
            !Guid.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("Token không hợp lệ.");
        }

        var query = new GetMeQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        var response = result.ToResponse();

        return Ok(ApiResponse.Success(response));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AuthContract.LoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var username = request.Username;
        _lockoutService.EnsureNotLockedOut(ipAddress, username);

        try
        {
            var command = request.ToCommand();
            var result = await _mediator.Send(command, cancellationToken);

            _lockoutService.ResetLockout(ipAddress, username);
            
            SetRefreshTokenCookie(
                result.RefreshToken,
                result.RefreshTokenExpiration);

            return Ok(
                ApiResponse.Success(result.ToResponse()));
        }
        catch (UnauthorizedAccessException)
        {
            _lockoutService.RecordFailedAttempt(ipAddress, username);
            throw;
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var refreshToken = ReadRefreshTokenCookie();

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var command = new LogoutCommand(refreshToken);
            await _mediator.Send(command, cancellationToken);
        }

        DeleteRefreshTokenCookies();
        return Ok(ApiResponse.Success(true));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [DisableRateLimiting]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var refreshToken = ReadRefreshTokenCookie();

        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new UnauthorizedAccessException("Không tìm thấy Refresh Token trong Cookie. Vui lòng đăng nhập lại.");
        }

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command, cancellationToken);

        SetRefreshTokenCookie(
            result.RefreshToken,
            result.RefreshTokenExpiration);

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpPost("google-login")]
    [AllowAnonymous]
    [EnableRateLimiting("InternalApiPolicy")]
    public async Task<IActionResult> GoogleLogin([FromBody] AuthContract.GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var command = request.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        SetRefreshTokenCookie(
            result.RefreshToken,
            result.RefreshTokenExpiration);

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    /// <summary>Stores the refresh token in a secure, server-only cookie.</summary>
    private void SetRefreshTokenCookie(
        string refreshToken,
        DateTime expiresAt)
    {
        var maxAge = expiresAt - DateTime.UtcNow;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = expiresAt,
            Secure = true,
            // The SPA can be hosted on a different site from the API. The
            // refresh request is credentialed, so its cookie must be allowed
            // on cross-site XHR/fetch requests as well.
            SameSite = SameSiteMode.None,
            Path = "/",
            MaxAge = maxAge > TimeSpan.Zero ? maxAge : TimeSpan.Zero
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    /// <summary>Reads the current cookie and supports the legacy name during migration.</summary>
    private string? ReadRefreshTokenCookie() =>
        Request.Cookies[RefreshTokenCookieName] ??
        Request.Cookies[LegacyRefreshTokenCookieName];

    /// <summary>Expires both current and legacy refresh-token cookies.</summary>
    private void DeleteRefreshTokenCookies()
    {
        var secureOptions = new CookieOptions
        {
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.None
        };

        Response.Cookies.Delete(
            ProductionRefreshTokenCookieName,
            secureOptions);
        Response.Cookies.Delete(
            LegacyRefreshTokenCookieName,
            secureOptions);
        Response.Cookies.Delete(LegacyRefreshTokenCookieName);
    }
}
