using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Api.Extensions;

namespace OVCMOVE.Api.Services;

public class HttpContextCurrentActorProvider(IHttpContextAccessor httpContextAccessor) : ICurrentActorProvider
{
    public string GetCurrentActor()
    {
        return (httpContextAccessor.HttpContext?.User)
            .GetCurrentUserDisplayName();
    }
}
