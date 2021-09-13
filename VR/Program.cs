using System.Threading.Tasks;
using Common.Config;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence;
using Persistence.DataStores;
using Persistence.Services;
using Scraper.Services;
using VR.Services;
using VRNotifier.Services;

namespace VR
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VRPersistenceDbContext>();
            await db.Database.MigrateAsync();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) => config.AddEnvironmentVariables("VR_"))
                .ConfigureServices((hostContext, services) =>
                {
                    // VRScraper
                    services.Configure<ScrapeSettings>(
                        hostContext.Configuration.GetSection(nameof(ScrapeSettings)));
                    services.AddScoped<IScrapeService, ScrapeService>();
                    services.AddHostedService<ScrapeDataFetcher>();

                    // VRNotifier
                    services.Configure<TrackedMediaSettings>(
                        hostContext.Configuration.GetSection(nameof(TrackedMediaSettings)));
                    services.Configure<DiscordSettings>(
                        hostContext.Configuration.GetSection($"NotifierSettings:{nameof(DiscordSettings)}"));

                    services.AddSingleton<DiscordSocketClient>();
                    services.AddSingleton<CommandService>();
                    services.AddSingleton<CommandHandlingService>();
                    services.AddSingleton<INotificationService, DiscordService>();
                    services.AddSingleton<DiscordService>();
                    services.AddHostedService<DiscordService>();

                    // VRPersistence
                    services.AddDbContext<VRPersistenceDbContext>(options =>
                            options.UseNpgsql(hostContext.Configuration.GetConnectionString("Db"),
                                o => { o.MigrationsAssembly(PersistenceAssemblyMarker.GetAssemblyName); }));
                    services.AddScoped<IReleaseService, ReleaseService>();
                    services.AddScoped<IReleaseDataStore, ReleaseDataStore>();
                    services.AddScoped<INotificationEndpointService, NotificationEndpointService>();
                    services.AddScoped<INotificationDataStore, NotificationDataStore>();
                    services.AddScoped<IMediaDataStore, MediaDataStore>();
                    services.AddScoped<ISubscriptionService, SubscriptionService>();
                    services.AddScoped<ISubscriptionDataStore, SubscriptionDataStore>();

                    // VROrchestrator
                    services.AddHostedService<VROrchestratorService>();
                });
    }
}