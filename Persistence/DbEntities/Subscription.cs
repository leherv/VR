using System.Collections.Generic;

namespace Persistence.DbEntities
{
    public class Subscription
    {
        public Media Media { get; set; }
        public int MediaId { get; set; }
        public NotificationEndpoint NotificationEndpoint { get; set; }
        public int NotificationEndpointId { get; set; }
        
        public Subscription(Media media, NotificationEndpoint notificationEndpoint)
        {
            Media = media;
            MediaId = media.Id;
            NotificationEndpoint = notificationEndpoint;
            NotificationEndpointId = notificationEndpoint.Id;
        }
        
        public Subscription() {}

        public BusinessEntities.Subscription ToBusinessEntity()
        {
            return new BusinessEntities.Subscription(
                new BusinessEntities.Media(Media.MediaName, Media.Description, new List<BusinessEntities.Subscription>()),
                new BusinessEntities.NotificationEndpoint(NotificationEndpoint.Identifier, new List<BusinessEntities.Subscription>())
            );
        }
    }
}