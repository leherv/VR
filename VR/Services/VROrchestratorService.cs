using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessEntities;
using Common.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Services;
using Scraper.Services;
using VR.Config;
using VRNotifier.Services;

namespace VR.Services
{
    public class VROrchestratorService: BackgroundService
    {
        private readonly OrchestratorServiceSettings _orchestratorServiceSettings;
        private readonly TrackedMediaSettings _trackedMediaSettings;
        private readonly ILogger<VROrchestratorService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IServiceProvider _serviceProvider;

        private List<ScrapeInstruction> _scrapeInstructions;
        private List<ScrapeInstruction> ScrapeInstructions => _scrapeInstructions ??= BuildScrapeInstructions();


        public VROrchestratorService(
            IOptions<OrchestratorServiceSettings> orchestratorServiceSettings,
            IOptions<TrackedMediaSettings> trackedMediaSettings,
            ILogger<VROrchestratorService> logger,
            IServiceProvider serviceProvider,
            INotificationService notificationService
        )
        {
            _trackedMediaSettings = trackedMediaSettings.Value;
            _orchestratorServiceSettings = orchestratorServiceSettings.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _notificationService = notificationService;
            if (!_trackedMediaSettings?.MediaNames?.Any() ?? true)
            {
                _logger.LogError($"{nameof(TrackedMediaSettings)}.{nameof(TrackedMediaSettings.MediaNames)} must be configured.");
                throw new ArgumentException($"{nameof(TrackedMediaSettings)}.{nameof(TrackedMediaSettings.MediaNames)} must be configured.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                try
                {
                    await OrchestrateAsync(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Something went wrong in {nameof(VROrchestratorService)}: {e.Message}\n {e.InnerException?.Message}");
                }
                await Task.Delay(
                    TimeSpan.FromMinutes(_orchestratorServiceSettings.ScrapeIntervalMinutes),
                    stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }

        private async Task OrchestrateAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var scrapeService = scope.ServiceProvider.GetRequiredService<IScrapeService>();
            _logger.LogInformation("Starting scraping...");
            var scrapeResults = await scrapeService.Scrape(ScrapeInstructions);
            _logger.LogInformation("Finished scraping");
            var successfulScrapeResults = scrapeResults
                .Where(result => result.IsSuccess)
                .Select(result => result.Value)
                .ToList();

            var releaseService = scope.ServiceProvider.GetRequiredService<IReleaseService>();
            _logger.LogInformation("Starting persisting of scrape results");
            var persistResults = await releaseService.AddReleases(ScrapeResult.ConvertToReleases(successfulScrapeResults), cancellationToken);
            // select only those scrapeResults that where successfully persisted and use the scrape info
            var successFullyPersistedScrapeResults = successfulScrapeResults.Zip(persistResults)
                .Where(tuple => tuple.Second.IsSuccess)
                .Select(tuple => tuple.First);
                
            _logger.LogInformation("Finished persisting scrape results.");

            var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            foreach (var successFullyPersistedScrapeResult in successFullyPersistedScrapeResults)
            {
                var subscribedEndpointsResult = await subscriptionService.GetSubscribedNotificationEndpoints(successFullyPersistedScrapeResult?.MediaName.ToLower(), cancellationToken);
                if (subscribedEndpointsResult.IsFailure)
                {
                    _logger.LogError("Fetching subscribed endpoints for media {mediaName} failed due to {message}", successFullyPersistedScrapeResult?.MediaName, subscribedEndpointsResult.Error);
                    continue;
                }
                    
                _logger.LogInformation("Notifying endpoints for new release of media {mediaName}", successFullyPersistedScrapeResult?.MediaName);
                var notificationInfo = new NotificationInfo(
                    successFullyPersistedScrapeResult.ReleaseNumber,
                    successFullyPersistedScrapeResult.SubReleaseNumber ?? 0,
                    successFullyPersistedScrapeResult.Url,
                    subscribedEndpointsResult.Value.Select(n => n.Identifier).ToList(),
                    successFullyPersistedScrapeResult.MediaName.ToLower());
                var notifyResult = await _notificationService.Notify(notificationInfo);

                if (notifyResult.IsFailure)
                    _logger.LogError("Notifying endpoints for new release of media {mediaName} failed due to {message}", successFullyPersistedScrapeResult?.MediaName, subscribedEndpointsResult.Error);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(VROrchestratorService)} is starting.");
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(VROrchestratorService)} is stopping.");
            await base.StopAsync(stoppingToken);
        }
        
        private List<ScrapeInstruction> BuildScrapeInstructions()
        {
            return _trackedMediaSettings.MediaNames
                    .Select(mediaName => new ScrapeInstruction(mediaName.ToLower()))
                    .ToList();
        }
    }
}