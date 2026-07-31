namespace OVCMOVE.Application.Abstractions.Services;

public interface IGoogleAuthService
{
    Task<GoogleUserProfile?> ValidateGoogleTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default);
}

public sealed record GoogleUserProfile(string Email, string? DisplayName, string? AvatarUrl);
