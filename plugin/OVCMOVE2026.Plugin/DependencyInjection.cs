using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Reflection; 
using OVCMOVE.Application.Abstractions.Plugins;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Options;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services;
using OVCMOVE2026.Plugin.Services.QrCode;

namespace OVCMOVE2026.Plugin;

/// <summary>Registers the optional MOVE 2026 feature module.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMove2026Plugin(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ISecretMissionRepository, SecretMissionRepository>();
        services.AddScoped<IQrCodeGeneratorService, QrCodeGeneratorService>();
        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "MongoDb:ConnectionString is required when the MOVE 2026 plugin is installed.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                "MongoDb:DatabaseName is required.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(provider =>
            new MongoClient(provider.GetRequiredService<IOptions<MongoDbOptions>>().Value.ConnectionString));
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
        });
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return provider.GetRequiredService<IMongoDatabase>().GetCollection<RaceCardDocument>(options.CollectionName);
        });
        services.AddScoped<IRaceCardRepository, MongoRaceCardRepository>();
        services.AddScoped<IRaceCardService, RaceCardService>();
        services.AddScoped<IPluginEventHandler, TrapBoothEntryRequestedHandler>();
        services.AddScoped<IPluginHub, Move2026PluginHub>();
        services.AddHostedService<ScheduledRestockWorker>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
