namespace BusinessEntities
{
    public class ScrapeInstruction
    {
        public string MediaName { get; set; }

        public ScrapeInstruction(string mediaName)
        {
            MediaName = mediaName;
        }
    }
}