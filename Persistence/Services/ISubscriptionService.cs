using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;

namespace Persistence.Services
{
    public interface ISubscriptionService
    {
        Task<Result> AddSubscription(Subscription subscription);
        Task<List<Result>> AddSubscriptions(List<Subscription> subscriptions);
        Task<Result<List<NotificationEndpoint>>> GetSubscribedNotificationEndpoints(string mediaName);
        Task<Result<List<Media>>> GetSubscribedToMedia(string notificationEndpointId);
        Task<Result> DeleteSubscription(DeleteSubscriptionInstruction deleteSubscriptionInstruction);
        Task<List<Result>> DeleteSubscriptions(List<DeleteSubscriptionInstruction> deleteSubscriptionInstructions);
    }
}