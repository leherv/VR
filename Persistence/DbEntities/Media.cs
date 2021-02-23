namespace Persistence.DbEntities
{
    public class Media
    {
        public long Id { get; set; }
        public string MediaName { get; set; }
        public string Description { get; set; }

        public Media(BusinessEntities.Media media)
        {
            MediaName = media.MediaName;
            Description = media.Description;
        }
        
        public Media() {}

        public BusinessEntities.Media ToBusinessEntity()
        {
            return new BusinessEntities.Media
            {
                Description = Description,
                Id = Id,
                MediaName = MediaName
            };
        }
    }
}