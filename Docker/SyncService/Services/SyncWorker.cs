using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SyncService.Services
{
    public class SyncWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SyncWorker> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromHours(24); // Sync every 24 hours

        public SyncWorker(IServiceProvider serviceProvider, ILogger<SyncWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sync Worker starting...");

            // Run initial sync
            await PerformSync();

            // Then run periodically
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_syncInterval, stoppingToken);
                if (!stoppingToken.IsCancellationRequested)
                {
                    await PerformSync();
                }
            }
        }

        private async Task PerformSync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<DataSyncService>();

                _logger.LogInformation("Starting data sync...");
                await syncService.SyncAllAsync();
                _logger.LogInformation("Data sync completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data sync");
            }
        }
    }
}