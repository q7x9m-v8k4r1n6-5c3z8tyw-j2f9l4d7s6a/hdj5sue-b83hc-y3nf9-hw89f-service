namespace OVCMOVE.Api.Extensions;

public static class CorsExtension
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        var cleanedOrigins = allowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (cleanedOrigins.Length == 0)
        {
            throw new InvalidOperationException("At least one CORS origin must be configured in Cors:AllowedOrigins.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(cleanedOrigins)
                      .AllowAnyHeader()   
                      .AllowAnyMethod()   
                      .AllowCredentials();
            });
        });

        return services;
    }
}
