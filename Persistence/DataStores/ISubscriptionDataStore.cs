using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public interface ISubscriptionDataStore
    {
        Task<Result> AddSubscription(Subscription subscription, CancellationToken cancellationToken);
        Task<Result<List<NotificationEndpoint>>> GetSubscribedNotificationEndpoints(string mediaName, CancellationToken cancellationToken);
        Task<Result<List<Media>>> GetSubscribedToMedia(string notificationEndpointId, CancellationToken cancellationToken);
        Task<Result<Subscription>> GetSubscription(long mediaId, long notificationEndpointId, CancellationToken cancellationToken);
        Task<Result> DeleteSubscription(Subscription subscription, CancellationToken cancellationToken);
    }
}