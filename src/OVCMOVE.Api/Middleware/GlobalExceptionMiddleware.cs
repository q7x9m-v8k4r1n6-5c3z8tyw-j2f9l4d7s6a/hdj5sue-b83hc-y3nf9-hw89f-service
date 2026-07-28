using System.Text.Json;
using OVCMOVE.Api.Common;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>Executes the request and converts unhandled exceptions to one API error format.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client ngắt kết nối trước khi API hoàn tất.");
            await WriteErrorAsync(
                context,
                499,
                "Client Closed Request");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Truy cập bị từ chối: {Message}", ex.Message);
            await WriteErrorAsync(
                context,
                ApiStatus.Codes.Unauthorized,
                ApiStatus.Messages.Unauthorized,
                ex.Message);
        }
        catch (ApplicationValidationException ex)
        {
            _logger.LogWarning("Yêu cầu không hợp lệ: {Message}", ex.Message);
            await WriteErrorAsync(
                context,
                ApiStatus.Codes.BadRequest,
                ApiStatus.Messages.BadRequest,
                ex.Message);
        }
        catch (ApplicationNotFoundException ex)
        {
            _logger.LogInformation("Không tìm thấy dữ liệu: {Message}", ex.Message);
            await WriteErrorAsync(
                context,
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound,
                ex.Message);
        }
        catch (ApplicationConflictException ex)
        {
            _logger.LogWarning("Xung đột cập nhật dữ liệu: {Message}", ex.Message);
            await WriteErrorAsync(
                context,
                ApiStatus.Codes.Conflict,
                ApiStatus.Messages.Conflict,
                ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Payload không hợp lệ: {Message}", ex.Message);
            await WriteErrorAsync(
                context,
                ApiStatus.Codes.BadRequest,
                ApiStatus.Messages.BadRequest,
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống không mong muốn: {Message}", ex.Message);
            var detail = _environment.IsDevelopment() ? ex.Message : string.Empty;
            await WriteErrorAsync(
                context,
                ApiStatus.Codes.InternalServerError,
                ApiStatus.Messages.InternalServerError,
                detail);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message,
        string detailError = "")
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var response = ApiResponse.Error(statusCode, message, detailError);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
