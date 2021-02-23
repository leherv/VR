using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public interface ISubscriptionDataStore
    {
        Task<Result> AddSubscription(Subscription subscription);
        Task<Result<List<NotificationEndpoint>>> GetSubscribedNotificationEndpoints(string mediaName);
        Task<Result<List<Media>>> GetSubscribedToMedia(string notificationEndpointId);
        Task<Result<Subscription>> GetSubscription(long mediaId, long notificationEndpointId);
        Task<Result> DeleteSubscription(Subscription subscription);
    }
}