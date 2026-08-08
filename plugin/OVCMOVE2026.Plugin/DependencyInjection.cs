using Microsoft.Extensions.DependencyInjection;
using System.Reflection; 
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services.QrCode;

namespace OVCMOVE2026.Plugin;

/// <summary>Registers the optional MOVE 2026 feature module.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMove2026Plugin(
        this IServiceCollection services)
    {
        services.AddScoped<ISecretMissionRepository, SecretMissionRepository>();
        services.AddScoped<IQrCodeGeneratorService, QrCodeGeneratorService>();
        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}