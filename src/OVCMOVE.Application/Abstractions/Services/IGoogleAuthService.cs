namespace OVCMOVE.Application.Abstractions.Services;

public interface IGoogleAuthService
{
    Task<string> ValidateGoogleTokenAndGetEmailAsync(string idToken);
}