using System.Threading;
using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;

namespace Persistence.Services
{
    public interface INotificationEndpointService
    {
        Task<Result> AddNotificationEndpoint(NotificationEndpoint notificationEndpoint, CancellationToken cancellationToken);
    }
}