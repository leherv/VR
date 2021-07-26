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
    public class SubscriptionDataStore : ISubscriptionDataStore
    {
        private readonly VRPersistenceDbContext _dbContext;
        private readonly ILogger<SubscriptionDataStore> _logger;

        public SubscriptionDataStore(VRPersistenceDbContext dbContext, ILogger<SubscriptionDataStore> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result> AddSubscription(Subscription subscription)
        {
            try
            {
                if (!await SubscriptionExists(subscription.Media.Id, subscription.NotificationEndpoint.Id))
                {
                    await _dbContext.Subscriptions.AddAsync(subscription);
                    await _dbContext.SaveChangesAsync();
                }

                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogInformation(
                    "Adding subscription for notificationEndpoint with identifier {identifier} for media {mediaName} failed due to: {exceptionMessage}. ",
                    subscription.NotificationEndpoint.Identifier,
                    subscription.Media.MediaName,
                    e.Message);
                return Result.Failure("Adding the subscription to the database failed.");
            }
        }

        public async Task<Result> DeleteSubscription(Subscription subscription)
        {
            try
            {
                _dbContext.Subscriptions.Remove(subscription);
                await _dbContext.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogInformation(
                    "Removing subscription for notificationEndpoint with identifier {identifier} for media {mediaName} failed due to: {exceptionMessage}. ",
                    subscription.NotificationEndpoint.Identifier,
                    subscription.Media.MediaName,
                    e.Message);
                return Result.Failure("Adding the subscription to the database failed.");
            }
                
        }

        private async Task<bool> SubscriptionExists(long mediaId, long notificationEndpointId)
        {
            return (await GetSubscription(mediaId, notificationEndpointId)).IsSuccess;
        }

        public async Task<Result<Subscription>> GetSubscription(long mediaId, long notificationEndpointId)
        {
            var subscription = await _dbContext.Subscriptions
                .Include(s => s.Media)
                .Include(s => s.NotificationEndpoint)
                .FirstOrDefaultAsync(s =>
                    s.Media.Id == mediaId &&
                    s.NotificationEndpoint.Id == notificationEndpointId
                );
            if (subscription == null)
            {
                return Result.Failure<Subscription>(
                    $"No subscription for mediaId {mediaId.ToString()} and notificationEndpointId {notificationEndpointId.ToString()} found.");
            }

            return Result.Success(subscription);
        }

        public async Task<Result<List<NotificationEndpoint>>> GetSubscribedNotificationEndpoints(string mediaName)
        {
            try
            {
                var notificationEndpoints = await _dbContext.Subscriptions
                    .Where(s => s.Media.MediaName.ToLower().Equals(mediaName.ToLower()))
                    .Include(s => s.NotificationEndpoint)
                    .Select(s => s.NotificationEndpoint)
                    .ToListAsync();
                return Result.Success(notificationEndpoints);
            }
            catch (Exception e)
            {
                _logger.LogInformation(
                    "Fetching subscribed endpoints for media {mediaName} failed due to: {exceptionMessage}. ",
                    mediaName,
                    e.Message);
                return Result.Failure<List<NotificationEndpoint>>(
                    "Adding the subscription to the database failed.");
            }
        }

        public async Task<Result<List<Media>>> GetSubscribedToMedia(string notificationEndpointIdentifier)
        {
            try
            {
                var media =  await _dbContext.Subscriptions
                    .Where(s => s.NotificationEndpoint.Identifier == notificationEndpointIdentifier)
                    .Select(s => s.Media)
                    .ToListAsync();
                return await Task.FromResult(media);
            }
            catch (Exception e)
            {
                _logger.LogInformation(
                    "Fetching media, which notificationEndpoint with identifier {identifier} is subscribed to, failed due to: {exceptionMessage}. ",
                    notificationEndpointIdentifier,
                    e.Message);
                return Result.Failure<List<Media>>(
                    $"Fetching media, which notificationEndpoint with identifier {notificationEndpointIdentifier} is subscribed to, failed due to: {e.Message}.");
            }
        }
    }
}