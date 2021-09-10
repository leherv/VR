using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Persistence.DataStores;
using Persistence.DbEntities;

namespace Persistence.Services
{
    public class NotificationEndpointService : INotificationEndpointService
    {
        private readonly INotificationDataStore _notificationDataStore;
        private readonly ILogger<NotificationEndpointService> _logger;

        public NotificationEndpointService(INotificationDataStore notificationDataStore, ILogger<NotificationEndpointService> logger)
        {
            _notificationDataStore = notificationDataStore;
            _logger = logger;
        }

        public async Task<Result> AddNotificationEndpoint(BusinessEntities.NotificationEndpoint notificationEndpoint, CancellationToken cancellationToken)
        {
            var notificationEndpointDao = new NotificationEndpoint(notificationEndpoint);
            return await _notificationDataStore.AddNotificationEndpoint(notificationEndpointDao, cancellationToken);
        }
    }
}