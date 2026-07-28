using Microsoft.AspNetCore.Authorization;
using OVCMOVE.Api.Security;

namespace OVCMOVE.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddRbacAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
