namespace BusinessEntities
{
    public class Media
    {
        public long Id { get; set; }
        public string MediaName { get; set; }
        public string Description { get; set; }

        public Media(string mediaName)
        {
            MediaName = mediaName;
        }

        public Media(string mediaName, string description)
        {
            MediaName = mediaName;
            Description = description;
        }

        public Media() {}
    }
}