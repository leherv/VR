using System.Collections.Generic;
using System.Linq;

namespace BusinessEntities
{
    public class ScrapeResult
    {
        public string MediaName { get; set; }
        public int ReleaseNumber { get; set; }
        public int? SubReleaseNumber { get; set; }
        public string Url { get; set; }

        public Release ToRelease()
        {
            return new Release(
                MediaName.ToLower(),
                ReleaseNumber,
                SubReleaseNumber ?? 0,
                Url
            );
        }
        
        public static List<Release> ConvertToReleases(IEnumerable<ScrapeResult> scrapeResults)
        {
            return scrapeResults
                .Select(scrapeResult => scrapeResult.ToRelease())
                .ToList();
        }
        
    }
}