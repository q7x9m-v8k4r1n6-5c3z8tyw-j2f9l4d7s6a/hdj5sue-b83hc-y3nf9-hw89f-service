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

    public GoogleAuthService(IOptions<GoogleAuthConfigOptions> googleAuthOption, ILogger<GoogleAuthService> logger)
    {
        _logger = logger;
        _clientId = googleAuthOption.Value.ClientId
            ?? throw new ArgumentNullException("Thiếu cấu hình GoogleAuth:ClientId"); 
    }

    public async Task<string> ValidateGoogleTokenAndGetEmailAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>() { _clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return payload.Email;
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Token Google bị sai hoặc hết hạn: {Message}", ex.Message);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError("Lỗi không xác định khi xác thực Google Token: {Message}", ex.Message);
            return string.Empty;
        }
    }
}