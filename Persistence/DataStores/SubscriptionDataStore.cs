﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public async Task<Result> AddSubscription(Subscription subscription, CancellationToken cancellationToken)
        {
            try
            {
                if (!await SubscriptionExists(subscription.MediaId, subscription.NotificationEndpointId, cancellationToken))
                {
                    await _dbContext.Subscriptions.AddAsync(subscription, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogInformation(
                    "Adding subscription for notificationEndpoint with id {id} for media with id {mediaId} failed due to: {exceptionMessage}. ",
                    subscription.NotificationEndpointId,
                    subscription.MediaId,
                    e.Message);
                return Result.Failure("Adding the subscription to the database failed.");
            }
        }

        public async Task<Result> DeleteSubscription(Subscription subscription, CancellationToken cancellationToken)
        {
            try
            {
                _dbContext.Subscriptions.Remove(subscription);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogInformation(
                    "Removing subscription for notificationEndpoint with id {id} for media {mediaId} failed due to: {exceptionMessage}. ",
                    subscription.NotificationEndpointId,
                    subscription.MediaId,
                    e.Message);
                return Result.Failure("Adding the subscription to the database failed.");
            }
                
        }

        private async Task<bool> SubscriptionExists(long mediaId, long notificationEndpointId, CancellationToken cancellationToken)
        {
            return (await GetSubscription(mediaId, notificationEndpointId, cancellationToken)).IsSuccess;
        }

        public async Task<Result<Subscription>> GetSubscription(long mediaId, long notificationEndpointId, CancellationToken cancellationToken)
        {
            var subscription = await _dbContext.Subscriptions
                .FirstOrDefaultAsync(s =>
                    s.MediaId == mediaId &&
                    s.NotificationEndpointId == notificationEndpointId,
                    cancellationToken
                );
            if (subscription == null)
            {
                return Result.Failure<Subscription>(
                    $"No subscription for mediaId {mediaId.ToString()} and notificationEndpointId {notificationEndpointId.ToString()} found.");
            }

            return Result.Success(subscription);
        }

        public async Task<Result<List<NotificationEndpoint>>> GetSubscribedNotificationEndpoints(string mediaName, CancellationToken cancellationToken)
        {
            try
            {
                var notificationEndpoints = await _dbContext.NotificationEndpoints
                    .Include(n => n.Subscriptions)
                    .ThenInclude(s => s.Media)
                    .Where(n => n.Subscriptions
                        .Select(s => s.Media.MediaName.ToLower())
                        .Any(m => m.Equals(mediaName.ToLower()))
                    )
                    .ToListAsync(cancellationToken);
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

        public async Task<Result<List<Media>>> GetSubscribedToMedia(string notificationEndpointIdentifier, CancellationToken cancellationToken)
        {
            try
            {
                var media =  await _dbContext.Subscriptions
                    .Where(s => s.NotificationEndpoint.Identifier == notificationEndpointIdentifier)
                    .Select(s => s.Media)
                    .ToListAsync(cancellationToken);
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