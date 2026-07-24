namespace OVCMOVE.Application.Abstractions.Services;

public interface IGoogleAuthService
{
    Task<GoogleUserProfile?> ValidateGoogleTokenAsync(string idToken);
}

public sealed record GoogleUserProfile(string Email, string? DisplayName);
