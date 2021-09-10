using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public class NotificationDataStore : INotificationDataStore
    {
        private readonly VRPersistenceDbContext _dbContext;
        private readonly ILogger<NotificationDataStore> _logger;

        public NotificationDataStore(VRPersistenceDbContext dbContext, ILogger<NotificationDataStore> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result> AddNotificationEndpoint(NotificationEndpoint notificationEndpoint, CancellationToken cancellationToken)
        {
            try
            {
                if (!await NotificationEndpointExists(notificationEndpoint.Identifier, cancellationToken))
                {
                    await _dbContext.AddAsync(notificationEndpoint, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
               
                return Result.Success();
            }
            catch (Exception e)
            {
                _logger.LogInformation("Adding notificationEndpoint with identifier {identifier} failed due to: {exceptionMessage}. ",
                    notificationEndpoint.Identifier,
                    e.Message);
                return Result.Failure("Adding the notificationEndpoint to the database failed.");
            }
        }
        
        private async Task<bool> NotificationEndpointExists(string identifier, CancellationToken cancellationToken)
        {
            return (await GetNotificationEndpoint(identifier, cancellationToken)).IsSuccess;
        }
        
        public async Task<Result<NotificationEndpoint>> GetNotificationEndpoint(string identifier, CancellationToken cancellationToken)
        {
            try
            {
                var endpoint = await _dbContext.NotificationEndpoints
                    .FirstOrDefaultAsync(n => n.Identifier.Equals(identifier), cancellationToken);
                 if (endpoint == null)
                 {
                     return Result.Failure<NotificationEndpoint>(
                         $"No notificationEndpoint for identifier {identifier} found.");
                 }

                 return Result.Success(endpoint);
            }
            catch (Exception e)
            {
                _logger.LogInformation("Fetching notificationEndpoint with identifier {identifier} failed due to: {exceptionMessage}.",
                    identifier,
                    e.Message);
                return Result.Failure<NotificationEndpoint>("Fetching the notificationEndpoint failed.");
            }
        }
    }
}