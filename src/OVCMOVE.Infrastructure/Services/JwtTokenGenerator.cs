using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtConfigOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtConfigOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public int RefreshTokenExpirationDays => _jwtOptions.RefreshTokenExpirationDays;

    public int AccessTokenExpirationMinutes => _jwtOptions.AccessTokenExpirationMinutes;

    public string GenerateAccessToken(User user, UserAccessProfileModel accessProfile)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.LinkedEmail),
            new Claim("user_type", user.UserType),
            new Claim("short_name", string.IsNullOrWhiteSpace(user.ShortName)
                ? OVCMOVE.Application.Common.ShortNameHelper.BuildBaseShortName(user.LinkedEmail)
                : user.ShortName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        foreach (var roleCode in accessProfile.Roles
                     .Select(role => role.Code)
                     .Where(code => !string.IsNullOrWhiteSpace(code))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
        }

        foreach (var permissionCode in accessProfile.Access
                     .Where(code => !string.IsNullOrWhiteSpace(code))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("permission", permissionCode));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey))
        {
            KeyId = _jwtOptions.SigningKeyId
        };
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }
}
