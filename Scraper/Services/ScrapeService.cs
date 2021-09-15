using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessEntities;
using CSharpFunctionalExtensions;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scraper.Config;

namespace Scraper.Services
{
    public class ScrapeService : IScrapeService
    {
        private readonly ILogger<ScrapeService> _logger;
        private readonly ScrapeSettings _scrapeSettings;
        
        public ScrapeService(ILogger<ScrapeService> logger, IOptions<ScrapeSettings> scrapeSettings)
        {
            _logger = logger;
            _scrapeSettings = scrapeSettings.Value;
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

        private async Task FetchResource(string url, string mediaName)
        {
            var fileName = $"{mediaName.ToLower()}.html";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var chromeExecutable = Path.Combine(_scrapeSettings.ChromePath, "chromium");
                var strCmdText =
                    $"--headless --disable-dev-shm-usage --disable-setuid-sandbox --no-sandbox --disable-gpu --dump-dom {url} > {fileName}";
                Console.WriteLine(strCmdText);
                var p = System.Diagnostics.Process.Start(chromeExecutable, strCmdText);
                if (p != null) await p.WaitForExitAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var chromeExecutable = Path.Combine(_scrapeSettings.ChromePath, "chrome.exe");
                var strCmdText =
                    $"/C \"{chromeExecutable}\" --headless --no-sandbox --disable-gpu --dump-dom {url} > {fileName} && icacls {fileName} /t /grant Everyone:F";
                var p =System.Diagnostics.Process.Start("CMD.exe", strCmdText);
                if (p != null) await p.WaitForExitAsync();
            }
            else
            {
                throw new SystemException("OS not supported");
            }
        }

        private async Task<Result<ScrapeResult>> ScrapeManganelo(string url, string mediaName)
        {
            try
            {
                _logger.LogInformation($"Fetching resource for media {mediaName}");
                await FetchResource(url, mediaName);
                _logger.LogInformation($"Finished fetching resource for media {mediaName}");

                _logger.LogInformation($"Starting scraping for media {mediaName}...");
                await using var fileStream = File.OpenRead($"{mediaName}.html");
                var html = new HtmlDocument();
                html.Load(fileStream);
                var document = html.DocumentNode;
                var newestLink = document.QuerySelector("div.panel-story-chapter-list ul.row-content-chapter > li.a-h > a");
                var chapterUrl = newestLink.GetAttributeValue("href", null);
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