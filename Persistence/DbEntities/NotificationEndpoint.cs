namespace Persistence.DbEntities
{
    public class NotificationEndpoint
    {
        public long Id { get; set; }
        public string Identifier { get; set; }

        public NotificationEndpoint(BusinessEntities.NotificationEndpoint nEndpoint)
        {
            Identifier = nEndpoint.Identifier;
        }
        
        public NotificationEndpoint() {}

        public BusinessEntities.NotificationEndpoint ToBusinessEntity()
        {
            return new BusinessEntities.NotificationEndpoint(Identifier);
        }
    }
}