using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OVCMOVE.Infrastructure.Options;

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

        var jwtOptions = configuration.GetSection(JwtConfigOptions.SectionName).Get<JwtConfigOptions>()
            ?? throw new InvalidOperationException("JwtConfig is not configured.");
        var secretKey = jwtOptions.SecretKey;

        // Kiểm tra Fail-Fast: Quăng lỗi ngay lập tức nếu Key không hợp lệ
        if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException("JwtConfig:SecretKey must be at least 32 bytes.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKeyId))
            throw new InvalidOperationException("JwtConfig:SigningKeyId is required for signing-key rotation.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
            throw new InvalidOperationException("JwtConfig:Issuer and JwtConfig:Audience are required.");

        if (jwtOptions.AccessTokenExpirationMinutes is < 1 or > 15)
            throw new InvalidOperationException("JwtConfig:AccessTokenExpirationMinutes must be between 1 and 15.");

        if (jwtOptions.RefreshTokenExpirationDays is < 1 or > 30)
            throw new InvalidOperationException("JwtConfig:RefreshTokenExpirationDays must be between 1 and 30.");

        var signingKeys = new Dictionary<string, SecurityKey>(StringComparer.Ordinal)
        {
            [jwtOptions.SigningKeyId] = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            {
                KeyId = jwtOptions.SigningKeyId
            }
        };

        var hasPreviousKey = !string.IsNullOrWhiteSpace(jwtOptions.PreviousSigningKeyId)
            || !string.IsNullOrWhiteSpace(jwtOptions.PreviousSecretKey);
        if (hasPreviousKey)
        {
            if (string.IsNullOrWhiteSpace(jwtOptions.PreviousSigningKeyId)
                || string.IsNullOrWhiteSpace(jwtOptions.PreviousSecretKey)
                || Encoding.UTF8.GetByteCount(jwtOptions.PreviousSecretKey) < 32)
                throw new InvalidOperationException("Previous JWT signing key ID and secret must both be configured; the secret must be at least 32 bytes.");

            signingKeys.Add(jwtOptions.PreviousSigningKeyId,
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.PreviousSecretKey))
                {
                    KeyId = jwtOptions.PreviousSigningKeyId
                });
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
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromMinutes(1),
                    IssuerSigningKeyResolver = (_, _, keyId, _) =>
                        keyId is not null && signingKeys.TryGetValue(keyId, out var key)
                            ? [key]
                            : [],
                };
            });

        return services;
    }
}
