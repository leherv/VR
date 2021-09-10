using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public interface INotificationDataStore
    {
        Task<Result> AddNotificationEndpoint(NotificationEndpoint notificationEndpoint, CancellationToken cancellationToken);
        Task<Result<NotificationEndpoint>> GetNotificationEndpoint(string identifier, CancellationToken cancellationToken);
    }
}