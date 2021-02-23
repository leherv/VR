namespace BusinessEntities
{
    public class Release
    {
        public Media Media { get; set; } 
        public int ReleaseNumber { get; set; }
        public int SubReleaseNumber { get; set; }
        public string Url { get; set; }
        public bool Notified { get; set; }

        public bool IsNewerThan(Release release)
        {
            if (ReleaseNumber > release.ReleaseNumber) return true;
            if (ReleaseNumber == release.ReleaseNumber)
            {
                return SubReleaseNumber > release.SubReleaseNumber;
            }

            return false;
        }
        
        public Release() {}

        public Release(string mediaName, int releaseNumber, int subReleaseNumber, string url, bool notified = false)
        {
            Media = new Media(mediaName);
            ReleaseNumber = releaseNumber;
            SubReleaseNumber = subReleaseNumber;
            Url = url;
            Notified = notified;
        }
    }
}