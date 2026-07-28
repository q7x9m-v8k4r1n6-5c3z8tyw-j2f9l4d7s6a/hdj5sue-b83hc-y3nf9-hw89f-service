using System.Security.Claims;
using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Api.Services;

public class HttpContextCurrentActorProvider(IHttpContextAccessor httpContextAccessor) : ICurrentActorProvider
{
    public string GetCurrentActor()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "system";
        }

        return user.FindFirst("short_name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? "system";
    }
}
