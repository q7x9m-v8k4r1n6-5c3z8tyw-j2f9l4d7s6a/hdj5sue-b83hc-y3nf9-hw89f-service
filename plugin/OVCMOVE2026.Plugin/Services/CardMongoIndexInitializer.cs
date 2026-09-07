using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public sealed class CardMongoIndexInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<CardMongoIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IRaceCardRepository>()
            .EnsureIndexesAsync(cancellationToken);
        logger.LogInformation("Mongo card indexes are ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
