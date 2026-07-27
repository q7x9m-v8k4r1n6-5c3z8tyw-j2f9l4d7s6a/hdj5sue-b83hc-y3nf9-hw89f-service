using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly string _clientId;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        IOptions<GoogleAuthConfigOptions> googleAuthOption,
        ILogger<GoogleAuthService> logger)
    {
        _logger = logger;
        _clientId = googleAuthOption.Value.ClientId
            ?? throw new ArgumentNullException("Thiếu cấu hình GoogleAuth:ClientId");
    }

    /// <summary>Validates a Google ID token for the configured client.</summary>
    public async Task<GoogleUserProfile?> ValidateGoogleTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_clientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            cancellationToken.ThrowIfCancellationRequested();

            return new GoogleUserProfile(payload.Email, payload.Name);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Token Google bị sai hoặc hết hạn: {Message}", ex.Message);
            return null;
        }
    }
}
