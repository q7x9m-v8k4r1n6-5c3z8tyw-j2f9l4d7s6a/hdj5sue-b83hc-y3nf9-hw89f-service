using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Infrastructure.BackgroundJobs;

public sealed class CleanupOldTokenService : BackgroundService
{
    private const int DaysToKeep = 14;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);

    private readonly ILogger<CleanupOldTokenService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CleanupOldTokenService(
        ILogger<CleanupOldTokenService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>Removes expired refresh-token history once per day.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var refreshTokenRepository = scope.ServiceProvider
                    .GetRequiredService<IRefreshTokenRepository>();

                var deletedCount = await refreshTokenRepository
                    .CleanupOldTokensAsync(DaysToKeep, stoppingToken);
                _logger.LogInformation(
                    "Removed {DeletedCount} expired refresh tokens.",
                    deletedCount);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Refresh-token cleanup failed.");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }
}
