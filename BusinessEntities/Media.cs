using System.Collections.Generic;

namespace BusinessEntities
{
    public class Media
    {
        public string MediaName { get; set; }
        public string Description { get; set; }
        public IList<Subscription> Subscriptions { get; set; }
        
        public Media(string mediaName, string description, IList<Subscription> subscriptions)
        {
            MediaName = mediaName;
            Description = description;
            Subscriptions = subscriptions;
        }

        public Media() {}
    }
}