using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using OVCMOVE.Api.Common;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Application.Features.Auth.Command.Logout;
using OVCMOVE.Application.Features.Auth.Command.Refresh;
using OVCMOVE.Application.Features.Auth.Command.GoogleLogin;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1;

public class AuthController : BaseController<AuthController>
{
    public AuthController(ILogger<AuthController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    // POST host:port/api/v1/Auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(new ApiResponseModel<LoginResponse>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Đăng nhập thất bại: {Message}", ex.Message);
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("Lỗi hệ thống khi Login: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    //POST host:port/api/v1/Auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(new ApiResponseModel<bool>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Lỗi hệ thống khi Logout: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    //POST host:port/api/v1/Auth/refresh-token
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(new ApiResponseModel<LoginResponse>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Refresh token thất bại: {Message}", ex.Message);
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("Lỗi hệ thống khi Refresh Token: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    // POST host:port/api/v1/Auth/google-login
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(new ApiResponseModel<LoginResponse>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Đăng nhập Google thất bại: {Message}", ex.Message);
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError("Lỗi hệ thống khi đăng nhập Google: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    // GET host:port/api/v1/Auth/test-rbac
    [HttpGet("test-rbac")]
    [Authorize(Roles = "Organizer")] // CẮM BIỂN CẤM: Bắt buộc phải có Token, và Role phải là Team hoặc Organizer
    public IActionResult TestRbac()
    {
        // Nhờ có trạm gác giải mã, ta có thể lấy thông tin trực tiếp từ Object "User" của C#
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new ApiResponseModel<object>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = "Chúc mừng! Bạn đã lọt qua chốt chặn RBAC an toàn.",
            Data = new 
            { 
                Email = userEmail, 
                Role = userRole,
                Note = "Chỉ những người có Token xịn mới nhìn thấy dòng này."
            }
        });
    }
}