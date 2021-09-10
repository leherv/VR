using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;

namespace Persistence.Services
{
    public interface IReleaseService
    {
        Task<Result<IEnumerable<Release>>> GetNotNotified(string mediaName, CancellationToken cancellationToken);
        Task<List<Result>> AddReleases(IEnumerable<Release> release, CancellationToken cancellationToken);
        Task<Result> AddRelease(Release release, CancellationToken cancellationToken);
        Task<List<Result>> SetNotified(IEnumerable<SetNotified> setNotified, CancellationToken cancellationToken);
    }
}