namespace Persistence.DbEntities
{
    public class Subscription
    {
        public long Id { get; set; }
        public long MediaId { get; set; }
        public long NotificationEndpointId { get; set; }
        public Media Media { get; set; }
        public NotificationEndpoint NotificationEndpoint { get; set; }

        public Subscription(BusinessEntities.Subscription subscription)
        {
            Media = new Media(subscription.Media);
            NotificationEndpoint = new NotificationEndpoint(subscription.NotificationEndpoint);
        }
        
        public Subscription() {}

        public BusinessEntities.Subscription ToBusinessEntity()
        {
            return new BusinessEntities.Subscription(Media.ToBusinessEntity(), NotificationEndpoint.ToBusinessEntity());
        }
    }
}