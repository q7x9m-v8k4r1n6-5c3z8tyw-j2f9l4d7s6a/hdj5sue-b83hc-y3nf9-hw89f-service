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

                // Xử lý Retry nếu gặp sự cố Cold Start từ Azure SQL
                int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var deletedCount = await refreshTokenRepository.CleanupOldTokensAsync(14);
                        _logger.LogInformation("✅ Đã dọn dẹp thành công {Count} token hết hạn.", deletedCount);
                        break; 
                    }
                    catch (Exception ex) when (attempt < maxRetries && !stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogWarning(ex, "⚠️ Lần thử {Attempt}/{MaxRetries} dọn dẹp Token thất bại (có thể do DB đang khởi động). Thử lại sau 5 giây...", attempt, maxRetries);
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi trong quá trình dọn dẹp Token.");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}