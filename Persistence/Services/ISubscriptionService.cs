using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;

namespace Persistence.Services
{
    public interface ISubscriptionService
    {
        Task<Result> AddSubscription(Subscription subscription, CancellationToken cancellationToken);
        Task<List<Result>> AddSubscriptions(List<Subscription> subscriptions, CancellationToken cancellationToken);
        Task<Result<List<NotificationEndpoint>>> GetSubscribedNotificationEndpoints(string mediaName, CancellationToken cancellationToken);
        Task<Result<List<Media>>> GetSubscribedToMedia(string notificationEndpointId, CancellationToken cancellationToken);
        Task<Result> DeleteSubscription(DeleteSubscriptionInstruction deleteSubscriptionInstruction, CancellationToken cancellationToken);
        Task<List<Result>> DeleteSubscriptions(List<DeleteSubscriptionInstruction> deleteSubscriptionInstructions, CancellationToken cancellationToken);
    }
}