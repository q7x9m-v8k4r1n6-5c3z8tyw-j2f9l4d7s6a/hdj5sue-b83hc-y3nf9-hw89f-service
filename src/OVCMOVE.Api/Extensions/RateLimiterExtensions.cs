using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OVCMOVE.Api.Common;

namespace OVCMOVE.Api.Extensions;

public static class RateLimiterExtensions
{
    private const string InternalApiPolicy = "InternalApiPolicy";

    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Xử lý Custom Response chuẩn JSON khi user bị chặn
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                // Trả về header báo thời gian chờ 5s
                context.HttpContext.Response.Headers.RetryAfter = "5";

                var response = ApiResponse.Error(
                    StatusCodes.Status429TooManyRequests,
                    "Too Many Requests",
                    "Bạn thao tác quá nhanh. Vui lòng đợi 5 giây trước khi thử lại.");

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    cancellationToken);
            };

            // Định nghĩa thuật toán Sliding Window Counter
            options.AddSlidingWindowLimiter(InternalApiPolicy, limiterOptions =>
            {
                limiterOptions.PermitLimit = 60; // 60 requests
                limiterOptions.Window = TimeSpan.FromMinutes(1); // Trong 1 phút
                limiterOptions.SegmentsPerWindow = 6; // Chia nhỏ mỗi 10s trượt 1 lần
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0; // Vượt 60 req là ném lỗi 429 luôn, không bắt hàng đợi
            });
        });

        return services;
    }
}