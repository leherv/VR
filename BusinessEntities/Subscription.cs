using System.Collections.Generic;

namespace BusinessEntities
{
    public class Subscription
    {
        public Media Media { get; set; }
        public NotificationEndpoint NotificationEndpoint { get; set; }

        public Subscription(Media media, NotificationEndpoint notificationEndpoint)
        {
            Media = media;
            NotificationEndpoint = notificationEndpoint;
        }

        public Subscription(string mediaName, string notificationEndpointIdentifier)
        {
            Media = new Media(mediaName, string.Empty, new List<Subscription>());
            NotificationEndpoint = new NotificationEndpoint(notificationEndpointIdentifier, new List<Subscription>());
        }
    }
}