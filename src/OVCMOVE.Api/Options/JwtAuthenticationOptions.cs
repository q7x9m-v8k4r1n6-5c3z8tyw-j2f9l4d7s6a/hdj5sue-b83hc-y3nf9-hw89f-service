namespace OVCMOVE.Api.Options;

/// <summary>API-owned JWT validation settings bound from the JwtConfig section.</summary>
public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "JwtConfig";

    public string SecretKey { get; set; } = string.Empty;
    public string SigningKeyId { get; set; } = string.Empty;
    public string? PreviousSecretKey { get; set; }
    public string? PreviousSigningKeyId { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
}
