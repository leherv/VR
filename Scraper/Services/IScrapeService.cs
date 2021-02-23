using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;

namespace Scraper.Services
{
    public interface IScrapeService
    {
        Task<Result<ScrapeResult>> Scrape(ScrapeInstruction scrapeInstruction);
        Task<List<Result<ScrapeResult>>> Scrape(List<ScrapeInstruction> scrapeInstructions);
    }
}