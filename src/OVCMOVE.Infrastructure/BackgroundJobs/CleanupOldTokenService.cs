using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Infrastructure.BackgroundJobs;

public class CleanupOldTokenService : BackgroundService
{
    private readonly ILogger<CleanupOldTokenService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CleanupOldTokenService(ILogger<CleanupOldTokenService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                
                var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

                var deletedCount = await refreshTokenRepository.CleanupOldTokensAsync(14);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi trong quá trình dọn dẹp Token.");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}