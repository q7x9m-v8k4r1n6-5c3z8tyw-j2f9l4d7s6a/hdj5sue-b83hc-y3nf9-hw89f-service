namespace OVCMOVE.Infrastructure.Options;

public class JwtConfigOptions
{
    public const string SectionName = "JwtConfig";
    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; }
    public int RefreshTokenExpirationDays { get; init; }
    public string SigningKeyId { get; init; } = string.Empty;
    public string PreviousSigningKeyId { get; init; } = string.Empty;
    public string PreviousSecretKey { get; init; } = string.Empty;
}
