using Microsoft.Extensions.DependencyInjection;

namespace OVCMOVE2026.Plugin;

/// <summary>Registers the optional MOVE 2026 feature module.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMove2026Plugin(
        this IServiceCollection services)
    {
        // Register plugin-owned handlers and services here as the module grows.
        return services;
    }
}
