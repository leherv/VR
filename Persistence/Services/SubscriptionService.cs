using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Persistence.DataStores;
using Subscription = Persistence.DbEntities.Subscription;

namespace Persistence.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionDataStore _subscriptionDataStore;
        private readonly IMediaDataStore _mediaDataStore;
        private readonly INotificationDataStore _notificationDataStore;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(ILogger<SubscriptionService> logger, ISubscriptionDataStore subscriptionDataStore, IMediaDataStore mediaDataStore, INotificationDataStore notificationDataStore)
        {
            _logger = logger;
            _subscriptionDataStore = subscriptionDataStore;
            _mediaDataStore = mediaDataStore;
            _notificationDataStore = notificationDataStore;
        }

        public async Task<Result> AddSubscription(BusinessEntities.Subscription subscription)
        {
            var subscriptionDao = new Subscription(subscription);
            var mediaResult = await _mediaDataStore.GetMedia(subscription.Media.MediaName);
            if (mediaResult.IsFailure)
            {
                _logger.LogError("Media {media} that should be subscribed to does not exist.", subscription.Media.MediaName);
                return Result.Failure(
                    $"Media {subscription.Media.MediaName} that should be subscribed to does not exist.");
            }
            var notificationEndpointResult =
                await _notificationDataStore.GetNotificationEndpoint(subscription.NotificationEndpoint.Identifier);
            if (notificationEndpointResult.IsFailure)
            {
                _logger.LogError("NotificationEndpoint {endpoint} that should be used for the subscription to does not exist.", subscription.NotificationEndpoint.Identifier);
                return Result.Failure(
                    $"NotificationEndpoint {subscription.NotificationEndpoint.Identifier} that should be used for the subscription to does not exist.");
            }

            subscriptionDao.Media = mediaResult.Value;
            subscriptionDao.NotificationEndpoint = notificationEndpointResult.Value;
            return await _subscriptionDataStore.AddSubscription(subscriptionDao);
        }

        public async Task<List<Result>> AddSubscriptions(List<BusinessEntities.Subscription> subscriptions)
        {
            var tasks = subscriptions.Select(AddSubscription);
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        public async Task<Result> DeleteSubscription(DeleteSubscriptionInstruction deleteSubscriptionInstruction)
        {
            var mediaResult = await _mediaDataStore.GetMedia(deleteSubscriptionInstruction.MediaName);
            if (mediaResult.IsFailure)
            {
                _logger.LogError("Media with name {mediaName}, referenced in the subscription to delete, does not exist.", deleteSubscriptionInstruction.MediaName);
                return Result.Failure(
                    $"Media with name {deleteSubscriptionInstruction.MediaName}, referenced in the subscription to delete, does not exist.");
            }
            var notificationEndpointResult =
                await _notificationDataStore.GetNotificationEndpoint(deleteSubscriptionInstruction.NotificationEndpointIdentifier);
            if (notificationEndpointResult.IsFailure)
            {
                _logger.LogError("NotificationEndpoint with identifier {identifier}, referenced in the subscription to delete, does not exist.", deleteSubscriptionInstruction.NotificationEndpointIdentifier);
                return Result.Failure(
                    $"NotificationEndpoint with identifier {deleteSubscriptionInstruction.NotificationEndpointIdentifier}, referenced in the subscription to delete, does not exist.");
            }

            var subscriptionResult = await _subscriptionDataStore.GetSubscription(mediaResult.Value.Id, notificationEndpointResult.Value.Id);
            if (subscriptionResult.IsFailure)
            {
                _logger.LogError("There is no subscription for mediaName {mediaName} and notificationEndpointIdentifier {notificationEndpointIdentifier} to delete", deleteSubscriptionInstruction.MediaName, deleteSubscriptionInstruction.NotificationEndpointIdentifier);
                return Result.Failure($"There is no subscription for mediaName {deleteSubscriptionInstruction.MediaName} and notificationEndpointIdentifier {deleteSubscriptionInstruction.NotificationEndpointIdentifier} to delete");
            }
            
            return await _subscriptionDataStore.DeleteSubscription(subscriptionResult.Value);
        }

        public async Task<List<Result>> DeleteSubscriptions(List<DeleteSubscriptionInstruction> deleteSubscriptionInstructions)
        {
            var tasks = deleteSubscriptionInstructions.Select(DeleteSubscription);
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        public async Task<Result<List<BusinessEntities.NotificationEndpoint>>> GetSubscribedNotificationEndpoints(string mediaName)
        {
            var result = await _subscriptionDataStore.GetSubscribedNotificationEndpoints(mediaName);
            return result.IsSuccess
                ? Result.Success(result.Value.Select(e => e.ToBusinessEntity()).ToList())
                : Result.Failure<List<BusinessEntities.NotificationEndpoint>>(
                    $"Failed to get subscribed notificationEndpoints for media {mediaName}");
        }

        public async Task<Result<List<BusinessEntities.Media>>> GetSubscribedToMedia(string notificationEndpointId)
        {
            var result =  await _subscriptionDataStore.GetSubscribedToMedia(notificationEndpointId);
            return result.IsSuccess
                ? Result.Success(result.Value.Select(m => m.ToBusinessEntity()).ToList())
                : Result.Failure<List<BusinessEntities.Media>>(result.Error);
        }
    }
}