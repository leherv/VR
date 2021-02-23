using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public interface INotificationDataStore
    {
        Task<Result> AddNotificationEndpoint(NotificationEndpoint notificationEndpoint);
        Task<Result<NotificationEndpoint>> GetNotificationEndpoint(string identifier);
    }
}