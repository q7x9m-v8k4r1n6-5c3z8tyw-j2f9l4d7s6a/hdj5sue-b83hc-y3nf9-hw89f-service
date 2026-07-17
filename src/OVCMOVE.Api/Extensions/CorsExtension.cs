namespace OVCMOVE.Api.Extensions;

public static class CorsExtension
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        var cleanedOrigins = allowedOrigins.Select(origin => origin.TrimEnd('/')).ToArray();

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