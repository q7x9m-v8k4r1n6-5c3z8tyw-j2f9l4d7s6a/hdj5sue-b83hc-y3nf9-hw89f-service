using System.Security.Claims;

namespace OVCMOVE.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetCurrentUserDisplayName(
        this ClaimsPrincipal? user,
        string fallback = "system")
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return fallback;
        }

        return user.FindFirst("short_name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("name")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? fallback;
    }
}
