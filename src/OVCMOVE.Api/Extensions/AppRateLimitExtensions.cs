using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Options;

namespace OVCMOVE.Api.Extensions;

public static class AppRateLimitExtensions
{
    private const string InternalApiPolicy = "InternalApiPolicy";

    public static IServiceCollection AddAppRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitOptions = configuration
            .GetSection(AppRateLimitConfigOptions.SectionName)
            .Get<AppRateLimitConfigOptions>()
            ?? throw new InvalidOperationException("AppRateLimitConfig is not configured.");

        if (rateLimitOptions.PermitLimit <= 0 || rateLimitOptions.WindowMinutes <= 0 || rateLimitOptions.DefaultRetryAfterSeconds <= 0)
            throw new InvalidOperationException("AppRateLimitConfig thresholds and window time must be greater than zero.");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = ApiStatus.Codes.TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = ApiStatus.Codes.TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                int retryAfterSeconds = rateLimitOptions.DefaultRetryAfterSeconds;
                
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);

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
                limiterOptions.PermitLimit = rateLimitOptions.PermitLimit;
                limiterOptions.Window = TimeSpan.FromMinutes(rateLimitOptions.WindowMinutes);
                limiterOptions.SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = rateLimitOptions.QueueLimit;
            });
        });

        return services;
    }
}