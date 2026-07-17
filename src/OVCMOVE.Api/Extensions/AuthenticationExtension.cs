using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace OVCMOVE.Api.Extensions;

public static class AuthenticationExtension
{
    /// <summary>
    /// Adds JWT authentication services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the authentication services to.</param>
    /// <param name="configuration">The configuration to retrieve JWT settings.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {

        var secretKey = configuration["JwtConfig:SecretKey"];

        // Kiểm tra Fail-Fast: Quăng lỗi ngay lập tức nếu Key không hợp lệ
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("SecretKey cho JWT bị thiếu hoặc rỗng. Vui lòng kiểm tra lại cấu hình trong file .env hoặc appsettings.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true, 
                    ValidateIssuerSigningKey = true, 
                    ValidIssuer = configuration["JwtConfig:Issuer"],
                    ValidAudience = configuration["JwtConfig:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                };
            });

        return services;
    }
}