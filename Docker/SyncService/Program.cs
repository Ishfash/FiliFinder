using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SyncService.Data;
using SyncService.Services;

namespace SyncService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Add services
            builder.Services.AddDbContext<FilamentDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<DataSyncService>();
            builder.Services.AddHostedService<SyncWorker>();

            // Configure logging
            builder.Services.AddLogging(config =>
            {
                config.AddConsole();
                config.SetMinimumLevel(LogLevel.Information);
            });

            var host = builder.Build();

            Console.WriteLine("Starting Sync Service...");
            await host.RunAsync();
        }
    }
}