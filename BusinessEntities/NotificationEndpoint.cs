using System.Collections.Generic;

namespace BusinessEntities
{
    public class NotificationEndpoint
    {
        public string Identifier { get; set; }
        public List<Subscription> Subscriptions { get; set; }
        
        public NotificationEndpoint(string identifier, List<Subscription> subscriptions)
        {
            Identifier = identifier;
            Subscriptions = subscriptions;
        }

        public NotificationEndpoint() {}
    }
}