using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Services;

public interface IJwtTokenGenerator
{
    int RefreshTokenExpirationDays { get; }
    int AccessTokenExpirationMinutes {get;}
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();

}