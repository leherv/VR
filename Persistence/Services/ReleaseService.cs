using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Persistence.DataStores;
using Persistence.DbEntities;

namespace Persistence.Services
{
    public class ReleaseService: IReleaseService
    {
        private readonly IReleaseDataStore _releaseDataStore;
        private readonly IMediaDataStore _mediaDataStore;
        private readonly ILogger<ReleaseService> _logger;

        public ReleaseService(IReleaseDataStore releaseDataStore, ILogger<ReleaseService> logger, IMediaDataStore mediaDataStore)
        {
            _releaseDataStore = releaseDataStore;
            _logger = logger;
            _mediaDataStore = mediaDataStore;
        }

        public async Task<Result<IEnumerable<BusinessEntities.Release>>> GetNotNotified(string mediaName, CancellationToken cancellationToken)
        {
            var result = await _releaseDataStore.GetNotNotified(mediaName, cancellationToken);
            return result.IsSuccess 
                ? Result.Success(result.Value.Select(releaseDao => releaseDao.ToBusinessEntity()))
                : Result.Failure<IEnumerable<BusinessEntities.Release>>($"Failed to load non notified releases for media ${mediaName}");
        }
        
        public async Task<Result> AddRelease(BusinessEntities.Release release, CancellationToken cancellationToken)
        {
            if (await IsNewNewest(release, cancellationToken))
            {
                var releaseDao = new Release(release);
                var mediaResult = await _mediaDataStore.GetMedia(release.Media.MediaName, cancellationToken);
                if (mediaResult.IsSuccess)
                {
                    releaseDao.Media = mediaResult.Value;
                }
                _logger.LogInformation("Release with releaseNumber {releaseNumber}.{subReleaseNumber} is the newest for {mediaName} so it will be added" , release.ReleaseNumber.ToString(), release.SubReleaseNumber.ToString(), release.Media.MediaName);
                return await _releaseDataStore.AddRelease(releaseDao, cancellationToken);
            }
            _logger.LogInformation("Release with releaseNumber {releaseNumber} is not newer for {mediaName} so it will be discarded", release.ReleaseNumber.ToString(), release.Media.MediaName);
            return Result.Failure($"Release with releaseNumber {release.ReleaseNumber.ToString()} is not newer for {release.Media.MediaName}");
        }

        private async Task<bool> IsNewNewest(BusinessEntities.Release release, CancellationToken cancellationToken)
        {
            var currentNewestResult = await _releaseDataStore.GetNewestReleaseForMedia(release.Media.MediaName, cancellationToken);
            if (currentNewestResult.IsSuccess)
            {
                _logger.LogInformation("Current newest release for media with name {mediaName} has releaseNumber {releaseNumber} and subReleaseNumber {subReleaseNumber}", currentNewestResult.Value?.Media?.MediaName, currentNewestResult.Value?.ReleaseNumber.ToString(), currentNewestResult.Value?.SubReleaseNumber.ToString());
                // no release yet for this media
                if (currentNewestResult.Value == null) return true;
                return release.IsNewerThan(currentNewestResult.Value.ToBusinessEntity());
            }

            return false;
        }

        public async Task<List<Result>> AddReleases(IEnumerable<BusinessEntities.Release> releases, CancellationToken cancellationToken)
        {
            var results = new List<Result>();
            foreach (var release in releases)
            {
                results.Add(await AddRelease(release, cancellationToken));
            }
            return results;
        }

        public async Task<List<Result>> SetNotified(IEnumerable<BusinessEntities.SetNotified> setNotified, CancellationToken cancellationToken)
        {
            var results = new List<Result>();
            foreach (var s in setNotified)
            {
                var result = await _releaseDataStore.GetRelease(s.ReleaseId, cancellationToken);
                if (result.IsSuccess)
                {
                    var releaseDao = result.Value;
                    _logger.LogInformation("Found release to set notified for id {id}", s.ReleaseId.ToString());
                    if (releaseDao.Notified)
                    {
                        _logger.LogInformation("Release was already notified, nothing to do.");
                    }
                    results.Add(await _releaseDataStore.SetNotified(releaseDao, cancellationToken));
                }
                else
                {
                    results.Add(Result.Failure($"Could not find release with id {s.ReleaseId.ToString()}"));
                }
            }
            return results;
        }
    }
}