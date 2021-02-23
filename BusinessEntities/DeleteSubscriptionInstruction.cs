namespace BusinessEntities
{
    public class DeleteSubscriptionInstruction
    {
        public string MediaName { get; set; }
        public string NotificationEndpointIdentifier { get; set; }

        public DeleteSubscriptionInstruction(string mediaName, string notificationEndpointIdentifier)
        {
            MediaName = mediaName;
            NotificationEndpointIdentifier = notificationEndpointIdentifier;
        }
    }
}