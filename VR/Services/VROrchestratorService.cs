using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessEntities;
using Common.Config;
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
        private readonly IReleaseService _releaseService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly DiscordService _notificationService;
        private readonly IScrapeService _scrapeService;
        private readonly OrchestratorServiceSettings _orchestratorServiceSettings;
        private readonly TrackedMediaSettings _trackedMediaSettings;
        private readonly ILogger<VROrchestratorService> _logger;

        private List<ScrapeInstruction> _scrapeInstructions;
        private List<ScrapeInstruction> ScrapeInstructions => _scrapeInstructions ??= BuildScrapeInstructions();


        public VROrchestratorService(
            IOptions<OrchestratorServiceSettings> orchestratorServiceSettings,
            IOptions<TrackedMediaSettings> trackedMediaSettings,
            ILogger<VROrchestratorService> logger,
            IScrapeService scrapeService,
            IReleaseService releaseService,
            ISubscriptionService subscriptionService,
            DiscordService notificationService)
        {
            _trackedMediaSettings = trackedMediaSettings.Value;
            _orchestratorServiceSettings = orchestratorServiceSettings.Value;
            _logger = logger;
            _scrapeService = scrapeService;
            _releaseService = releaseService;
            _subscriptionService = subscriptionService;
            _notificationService = notificationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                try
                {
                    await ExecuteAsyncP(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError("Something went wrong: {exception}\n {innerException}", e.Message, e.InnerException?.Message);
                }
                await Task.Delay(TimeSpan.FromMinutes(_orchestratorServiceSettings.ScrapeIntervalMinutes),
                    stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }

        private async Task ExecuteAsyncP(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting scraping...");
            var scrapeResults = await _scrapeService.Scrape(ScrapeInstructions);
            _logger.LogInformation("Finished scraping");
            var successfulScrapeResults = scrapeResults
                .Where(result => result.IsSuccess)
                .Select(result => result.Value)
                .ToList();
            
            _logger.LogInformation("Starting persisting of scrape results");
            var persistResults = await _releaseService.AddReleases(ScrapeResult.ConvertToReleases(successfulScrapeResults));
            // select only those scrapeResults that where successfully persisted and use the scrape info
            var successFullyPersistedScrapeResults = successfulScrapeResults.Zip(persistResults)
                .Where(tuple => tuple.Second.IsSuccess)
                .Select(tuple => tuple.First);
            
            _logger.LogInformation("Finished persisting scrape results.");
            
            foreach (var successFullyPersistedScrapeResult in successFullyPersistedScrapeResults)
            {
                var subscribedEndpointsResult = await _subscriptionService.GetSubscribedNotificationEndpoints(successFullyPersistedScrapeResult?.MediaName.ToLower());
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
        
        private List<ScrapeInstruction> BuildScrapeInstructions()
        {
            return _trackedMediaSettings.MediaNames
                    .Select(mediaName => new ScrapeInstruction(mediaName.ToLower()))
                    .ToList();
        }
    }
}