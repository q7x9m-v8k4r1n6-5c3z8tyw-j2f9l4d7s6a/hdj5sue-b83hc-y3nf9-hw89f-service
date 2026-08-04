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
            options.RejectionStatusCode = ApiStatus.Codes.TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = ApiStatus.Codes.TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                int retryAfterSeconds = 5;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                }

                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

                var response = ApiResponse.Error(
                    ApiStatus.Codes.TooManyRequests, 
                    ApiStatus.Messages.TooManyRequests,
                    $"Bạn thao tác quá nhanh. Vui lòng đợi {retryAfterSeconds} giây trước khi thử lại.");

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    cancellationToken);
            };

            options.AddSlidingWindowLimiter(InternalApiPolicy, limiterOptions =>
            {
                limiterOptions.PermitLimit = 60;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.SegmentsPerWindow = 6;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        });

        return services;
    }
}