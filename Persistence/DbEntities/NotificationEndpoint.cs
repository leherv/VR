using System.Collections.Generic;
using System.Linq;

namespace Persistence.DbEntities
{
    public class NotificationEndpoint
    {
        public int Id { get; set; }
        public string Identifier { get; set; }
        public IList<Subscription> Subscriptions { get; set; }
        
        public NotificationEndpoint(BusinessEntities.NotificationEndpoint nEndpoint)
        {
            Identifier = nEndpoint.Identifier;
            Subscriptions = nEndpoint.Subscriptions
                .Select(s => new Subscription(new Media(s.Media), new NotificationEndpoint(s.NotificationEndpoint)))
                .ToList();
        }
        
        public NotificationEndpoint() {}

        public BusinessEntities.NotificationEndpoint ToBusinessEntity()
        {
            return new BusinessEntities.NotificationEndpoint(
                Identifier,
                Subscriptions.Select(s => s.ToBusinessEntity()).ToList()
            );
        }
    }
}