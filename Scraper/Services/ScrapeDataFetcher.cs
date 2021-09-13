using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BusinessEntities;
using Common.Config;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Scraper.Services
{
    public class ScrapeDataFetcher : BackgroundService
    {
        private readonly ILogger<ScrapeDataFetcher> _logger;
        private readonly TrackedMediaSettings _trackedMediaSettings;
        private readonly ScrapeSettings _scrapeSettings;
        private List<ScrapeInstruction> _scrapeInstructions;
        private IEnumerable<ScrapeInstruction> ScrapeInstructions => _scrapeInstructions ??= BuildScrapeInstructions();

        public ScrapeDataFetcher(
            ILogger<ScrapeDataFetcher> logger,
            IOptions<TrackedMediaSettings> trackedMediaSettings,
            IOptions<ScrapeSettings> scrapeSettings
        )
        {
            _logger = logger;
            _scrapeSettings = scrapeSettings.Value;
            _trackedMediaSettings = trackedMediaSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                try
                {
                    _logger.LogInformation("Starting fetch of different targets...");
                    foreach (var scrapeInstruction in ScrapeInstructions)
                    {
                        var urlResult = MapToResourceUrl(scrapeInstruction);
                        if (urlResult.IsFailure)
                        {
                            _logger.LogError(
                                $"Could not map ScrapeInstruction for media {scrapeInstruction?.MediaName ?? "NULL"} to URL.");
                            continue;
                        }

                        var fileName = $"{scrapeInstruction.MediaName.ToLower()}.html";
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            var chromeExecutable = Path.Combine(_scrapeSettings.ChromePath, "chromium");
                            _logger.LogInformation(chromeExecutable);
                            var strCmdText =
                                $"{chromeExecutable} --headless --no-sandbox --disable-gpu --dump-dom {urlResult.Value} > {fileName}";
                            _logger.LogInformation(strCmdText);
                            var p = System.Diagnostics.Process.Start("/bin/bash", strCmdText);
                            if (p != null) await p.WaitForExitAsync(stoppingToken);
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            var chromeExecutable = Path.Combine(_scrapeSettings.ChromePath, "chrome.exe");
                            var strCmdText =
                                $"/C \"{chromeExecutable}\" --headless --no-sandbox --disable-gpu --dump-dom {urlResult.Value} > {fileName} && icacls {fileName} /t /grant Everyone:F";
                            var p =System.Diagnostics.Process.Start("CMD.exe", strCmdText);
                            if (p != null) await p.WaitForExitAsync(stoppingToken);
                        }
                        else
                        {
                            throw new SystemException("OS not supported");
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Something went wrong in {nameof(ScrapeDataFetcher)}: {e.Message}\n {e.InnerException.Message}");
                }
                await Task.Delay(TimeSpan.FromMinutes(_scrapeSettings.ScrapeIntervalMinutes), stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }

        private static Result<string> MapToResourceUrl(ScrapeInstruction scrapeInstruction)
        {
            var result = scrapeInstruction.MediaName.ToLower() switch
            {
                "sololeveling" => "https://manganelo.com/manga/pn918005",
                "talesofdemonsandgods" => "https://manganelo.com/manga/hyer5231574354229",
                "martialpeak" => "https://manganelo.com/manga/martial_peak",
                "jujutsukaisen" => "https://manganelo.com/manga/jujutsu_kaisen",
                "skeletonsoldier" => "https://manganelo.com/manga/upzw279201556843676",
                "drstone" => "https://manganelo.com/manga/dr_stone",
                _ => Result.Failure<string>($"No scraper set up for media with name {scrapeInstruction.MediaName}")
            };
            return result;
        }
        
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(ScrapeDataFetcher)} is stopping.");
            await base.StopAsync(stoppingToken);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(ScrapeDataFetcher)} is starting.");
            return base.StartAsync(cancellationToken);
        }

        private List<ScrapeInstruction> BuildScrapeInstructions()
        {
            return _trackedMediaSettings.MediaNames
                .Select(mediaName => new ScrapeInstruction(mediaName.ToLower()))
                .ToList();
        }
    }
}