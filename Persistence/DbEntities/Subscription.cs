namespace Persistence.DbEntities
{
    public class Subscription
    {
        public long Id { get; set; }
        public Media Media { get; set; }
        public NotificationEndpoint NotificationEndpoint { get; set; }

        public Subscription(Media media, NotificationEndpoint notificationEndpoint)
        {
            Media = media;
            NotificationEndpoint = notificationEndpoint;
        }
        
        public Subscription() {}

        public BusinessEntities.Subscription ToBusinessEntity()
        {
            return new(Media.ToBusinessEntity(), NotificationEndpoint.ToBusinessEntity());
        }
    }
}