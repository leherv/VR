using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;

namespace VRNotifier.Services
{
    public interface INotificationService
    {
        public Task<Result> Notify(NotificationInfo notificationInfo);
    }
}