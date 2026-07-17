namespace OVCMOVE.Api.Extensions;

public static class CorsExtension
{
    /// <summary>
    /// Adds CORS policy services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the CORS services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5173",
                        "https://move.oispvolunteerclub.com"
                      )
                      .AllowAnyHeader()   
                      .AllowAnyMethod()   
                      .AllowCredentials();
            });
        });

        return services;
    }
}