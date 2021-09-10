using System.Collections.Generic;
using System.Linq;

namespace Persistence.DbEntities
{
    public class Media
    {
        public int Id { get; set; }
        public string MediaName { get; set; }
        public string Description { get; set; }
        
        public IList<Subscription> Subscriptions { get; set; }

        public Media(BusinessEntities.Media media)
        {
            MediaName = media.MediaName;
            Description = media.Description;
            Subscriptions = media.Subscriptions?.Select(s => new Subscription(new Media(s.Media), new NotificationEndpoint(s.NotificationEndpoint))).ToList();
        }
        
        public Media() {}

        public BusinessEntities.Media ToBusinessEntity()
        {
            return new BusinessEntities.Media(
                MediaName,
                Description,
                Subscriptions?.Select(s => s.ToBusinessEntity()).ToList()
            );
        }
    }
}