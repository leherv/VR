using System;
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

        public async Task<Result> AddNotificationEndpoint(NotificationEndpoint notificationEndpoint)
        {
            try
            {
                if (!await NotificationEndpointExists(notificationEndpoint.Identifier))
                {
                    await _dbContext.AddAsync(notificationEndpoint);
                    await _dbContext.SaveChangesAsync();
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
        
        private async Task<bool> NotificationEndpointExists(string identifier)
        {
            return (await GetNotificationEndpoint(identifier)).IsSuccess;
        }
        
        public async Task<Result<NotificationEndpoint>> GetNotificationEndpoint(string identifier)
        {
            try
            {
                var endpoint = await _dbContext.NotificationEndpoints
                    .FirstOrDefaultAsync(n => n.Identifier.Equals(identifier));
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