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
using VR.Config;
using VR.Services;
using VRNotifier.Services;

namespace VR
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) => config.AddEnvironmentVariables("VR_"))
                .ConfigureServices((hostContext, services) =>
                {
                    // VRScraper
                    services.AddSingleton<IScrapeService, ScrapeService>();

                    // VRNotifier
                    services.Configure<TrackedMediaSettings>(hostContext.Configuration.GetSection(nameof(TrackedMediaSettings)));
                    services.Configure<DiscordSettings>(
                        hostContext.Configuration.GetSection($"NotifierSettings:{nameof(DiscordSettings)}"));

                    services.AddSingleton<DiscordSocketClient>();
                    services.AddSingleton<CommandService>();
                    services.AddSingleton<CommandHandlingService>();
                    services.AddSingleton<DiscordService>();
                    services.AddHostedService(provider => provider.GetRequiredService<DiscordService>());

                    // VRPersistence
                    services.AddDbContext<VRPersistenceDbContext>(options =>
                        options.UseNpgsql(hostContext.Configuration.GetConnectionString("Db"), o =>
                        {
                            o.MigrationsAssembly(PersistenceAssemblyMarker.GetAssemblyName);
                        }), ServiceLifetime.Transient);
                    services.AddSingleton<IReleaseService, ReleaseService>();
                    services.AddSingleton<IReleaseDataStore, ReleaseDataStore>();
                    services.AddSingleton<INotificationEndpointService, NotificationEndpointService>();
                    services.AddSingleton<INotificationDataStore, NotificationDataStore>();
                    services.AddSingleton<IMediaDataStore, MediaDataStore>();
                    services.AddSingleton<ISubscriptionService, SubscriptionService>();
                    services.AddSingleton<ISubscriptionDataStore, SubscriptionDataStore>();

                    // VROrchestrator
                    services.Configure<OrchestratorServiceSettings>(
                        hostContext.Configuration.GetSection(nameof(OrchestratorServiceSettings)));
                    services.AddHostedService<VROrchestratorService>();
                });
    }
}