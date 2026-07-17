using System.Text.Json;
using OVCMOVE.Api.Common;

namespace OVCMOVE.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client ngắt kết nối trước khi API hoàn tất.");
            
            context.Response.StatusCode = 499; // 499 Client Closed Request
            context.Response.ContentType = "application/json";
            
            var result = JsonSerializer.Serialize(new { message = "Client Closed Request" });
            await context.Response.WriteAsync(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Truy cập bị từ chối: {Message}", ex.Message);
            
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            
            var result = JsonSerializer.Serialize(new { message = ex.Message });
            await context.Response.WriteAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống không mong muốn: {Message}", ex.Message);
            
            context.Response.StatusCode = StatusCodes.Status200OK; 
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var result = JsonSerializer.Serialize(new InternalServerErrorModel(ex.Message), options);
            
            await context.Response.WriteAsync(result);
        }
    }
}