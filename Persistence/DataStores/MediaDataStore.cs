using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public class MediaDataStore : IMediaDataStore
    {
        private readonly VRPersistenceDbContext _dbContext;
        private readonly ILogger<MediaDataStore> _logger;

        public MediaDataStore(ILogger<MediaDataStore> logger, VRPersistenceDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }
        
        public async Task<Result<Media>> GetMedia(string mediaName)
        {
            var media = await _dbContext.Media
                .Where(m => m.MediaName.ToLower().Equals(mediaName.ToLower()))
                .Take(1)
                .ToListAsync();
            return media.Count == 1
                ? Result.Success(media[0])
                : Result.Failure<Media>($"There are {media.Count.ToString()} media with mediaName {mediaName}.");
        }
    }
}