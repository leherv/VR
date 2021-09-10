using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public interface IReleaseDataStore
    {
        Task<Result<List<Release>>> GetNotNotified(string mediaName, CancellationToken cancellationToken);
        public Task<Result<Release>> GetRelease(string mediaName, int releaseNumber, CancellationToken cancellationToken);
        public Task<Result<Release>> GetRelease(long id, CancellationToken cancellationToken);
        public Task<Result> AddRelease(Release release, CancellationToken cancellationToken);
        public Task<Result<Release>> GetNewestReleaseForMedia(string mediaName, CancellationToken cancellationToken);
        public Task<Result> SetNotified(Release release, CancellationToken cancellationToken);
    }
}