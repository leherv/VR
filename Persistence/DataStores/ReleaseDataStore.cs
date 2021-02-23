using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public class ReleaseDataStore: IReleaseDataStore
    {
        private readonly VRPersistenceDbContext _dbContext;
        private readonly ILogger<ReleaseDataStore> _logger;

        public ReleaseDataStore(VRPersistenceDbContext dbContext, ILogger<ReleaseDataStore> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<List<Release>>> GetNotNotified(string mediaName)
        {
            var releases = await _dbContext.Releases
                .Include(r => r.Media)
                .Where(r => r.Media.MediaName.ToLower().Equals(mediaName.ToLower()) &&
                       !r.Notified)
                .ToListAsync();
            return Result.Success(releases);
        }
        
        public async Task<Result<Release>> GetRelease(string mediaName, int releaseNumber)
        {
            var release = await _dbContext.Releases
                .Include(r => r.Media)
                .FirstOrDefaultAsync(r => r.Media.MediaName == mediaName && r.ReleaseNumber == releaseNumber);
            return release == null ? Result.Failure<Release>("No release found") : Result.Success(release);
        }
        
        public async Task<Result<Release>> GetRelease(long id)
        {
            var release = await _dbContext.Releases
                .Include(r => r.Media)
                .FirstOrDefaultAsync(r => r.Id == id);
            return release == null ? Result.Failure<Release>("No release found") : Result.Success(release);
        }
        
        public async Task<Result> AddRelease(Release release)
        {
            try
            {
                await _dbContext.Releases.AddAsync(release);
                await _dbContext.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogInformation("Adding release with releaseNumber {releaseNumber} for {mediaName} failed due to: {exceptionMessage}. ", release.ReleaseNumber.ToString(), release.Media.MediaName, e.Message);
                return Result.Failure("Adding the release to the database failed.");
            }
        }
        
        public async Task<Result<Release>> GetNewestReleaseForMedia(string mediaName)
        {
            try
            {
                var newestRelease = await _dbContext.Releases
                    .Where(r => r.Media.MediaName.ToLower().Equals(mediaName.ToLower()))
                    .Include(r => r.Media)
                    .OrderByDescending(r => r.ReleaseNumber)
                    .Take(1)
                    .ToListAsync();
                return newestRelease.Count == 1
                    ? Result.Success(newestRelease[0])
                    : Result.Success<Release>(null);
            }
            catch (Exception e)
            {
                _logger.LogInformation("Getting newest release for media {media} failed due to: {exceptionMessage}", mediaName, e.Message);
                return Result.Failure<Release>($"Getting newest release for media {mediaName} failed.");
            }
        }

        public async Task<Result> SetNotified(Release release)
        {
            try
            {
                release.Notified = true;
                await _dbContext.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogInformation("Setting release with releaseNumber {releaseNumber} for {mediaName} to notified failed due to: {exceptionMessage}. ", release.ReleaseNumber.ToString(), release.Media.MediaName, e.Message);
                return Result.Failure("Setting the release to notified failed.");
            }
            
        }
    }
}