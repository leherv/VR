using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace Scraper.Services
{
    public class ScrapeService : IScrapeService
    {
        private readonly ILogger<ScrapeService> _logger;

        private readonly LaunchOptions _launchOptions = new()
        {
            Headless = true,
            ExecutablePath = "/usr/bin/chromium",
            Args = new[] {
                "--headless",
                "--no-sandbox"
            }
        };

        public ScrapeService(ILogger<ScrapeService> logger)
        {
            _logger = logger;
        }

        public async Task<Result<ScrapeResult>> Scrape(ScrapeInstruction scrapeInstruction)
        {
            var result = scrapeInstruction.MediaName.ToLower() switch
            {
                "sololeveling" => await ScrapeManganelo("https://manganelo.com/manga/pn918005", scrapeInstruction.MediaName.ToLower()),
                "talesofdemonsandgods" => await ScrapeManganelo("https://manganelo.com/manga/hyer5231574354229", scrapeInstruction.MediaName.ToLower()),
                "martialpeak" => await ScrapeManganelo("https://manganelo.com/manga/martial_peak", scrapeInstruction.MediaName.ToLower()),
                "jujutsukaisen" => await ScrapeManganelo("https://manganelo.com/manga/jujutsu_kaisen", scrapeInstruction.MediaName.ToLower()),
                "skeletonsoldier" => await ScrapeManganelo("https://manganelo.com/manga/upzw279201556843676", scrapeInstruction.MediaName.ToLower()),
                "drstone" => await ScrapeManganelo("https://manganelo.com/manga/dr_stone", scrapeInstruction.MediaName.ToLower()),
                _ => Result.Failure<ScrapeResult>($"No scraper set up for media with name {scrapeInstruction.MediaName}")
            };
            return result;
        }

        public async Task<List<Result<ScrapeResult>>> Scrape(List<ScrapeInstruction> scrapeInstructions)
        {
            var tasks = scrapeInstructions.Select(Scrape);
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        private async Task<Result<ScrapeResult>> ScrapeManganelo(string url, string mediaName)
        {
            try
            {
                _logger.LogInformation($"Starting scraping for media {mediaName}...");
                await using var browser = await Puppeteer.LaunchAsync(_launchOptions);
                await using var page = await browser.NewPageAsync();
                page.DefaultTimeout = 50000;
                await page.GoToAsync(url);
                var container = await page.WaitForSelectorAsync("div.panel-story-chapter-list",
                    new WaitForSelectorOptions {Visible = true});
                var newestLink = await container.QuerySelectorAsync("ul.row-content-chapter > li.a-h > a");
                var chapterUrlHandle = await newestLink.GetPropertyAsync("href");
                var chapterUrl = (string) await chapterUrlHandle.JsonValueAsync();
                var regexResult = Regex.Match(chapterUrl, @"chapter-(\d{1,4})\.*(\d{0,4})");
                var releaseNumberString = regexResult.Groups[1].Value;
                var subReleaseNumberString = regexResult.Groups[2].Value;
                if (!int.TryParse(releaseNumberString, out var releaseNumber))
                {
                    _logger.LogError("Releasenumber could not be extracted from link {chapterUrl} for media {mediaName}", chapterUrl, mediaName);
                    return Result.Failure<ScrapeResult>($"Releasenumber could not be extracted from link {chapterUrl} for media {mediaName}");
                }

                var subReleaseNumber = 0;
                if (!string.IsNullOrEmpty(subReleaseNumberString))
                {
                    if (!int.TryParse(subReleaseNumberString, out subReleaseNumber))
                    {
                        return Result.Failure<ScrapeResult>($"SubReleaseNumber could not be extracted from link {chapterUrl} for media {mediaName}");
                    }
                }
                _logger.LogInformation($"Successfully scraped for media {mediaName}");
                return Result.Success(new ScrapeResult
                {
                    MediaName = mediaName,
                    ReleaseNumber = releaseNumber,
                    SubReleaseNumber = subReleaseNumber,
                    Url = chapterUrl
                });
            }
            catch (Exception e)
            {
                _logger.LogError("Something went wrong while scraping for media {media}. {exceptionMessage}", mediaName, e.Message);
                return Result.Failure<ScrapeResult>($"Scraping for media {mediaName} failed due to {e.Message}");
            }
        }
    }
}