namespace Persistence.DbEntities
{
    public class Release
    {
        public long Id { get; set; }
        public Media Media { get; set; } 
        public int ReleaseNumber { get; set; }
        public int SubReleaseNumber { get; set; }
        public string Url { get; set; }
        public bool Notified { get; set; }

        public Release(BusinessEntities.Release release)
        {
            Media = new Media(release.Media);
            ReleaseNumber = release.ReleaseNumber;
            SubReleaseNumber = release.SubReleaseNumber;
            Url = release.Url;
            Notified = release.Notified;
        }

        public Release() {}

        public BusinessEntities.Release ToBusinessEntity()
        {
            return new BusinessEntities.Release
            {
                Media = Media.ToBusinessEntity(),
                Notified = Notified,
                Url = Url,
                ReleaseNumber = ReleaseNumber,
                SubReleaseNumber = SubReleaseNumber
            };
        }
    }
}