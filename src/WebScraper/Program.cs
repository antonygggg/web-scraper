using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Serilog;

namespace WebScraper
{
	public class Program
	{
		private static readonly BlockingCollection<ScrapeResult>
			_scrapeResults = new BlockingCollection<ScrapeResult>();

		private static IReadOnlyList<string> _webSites;

		public static void Main(string[] args)
		{
			InitLogger();

			LoadDomainsFromFile("web_sites.csv");

			var stopwatch = Stopwatch.StartNew();

			ScrapeAllDomains();

			stopwatch.Stop();

			PrintScrapeResult(stopwatch);
		}

		private static void ScrapeAllDomains()
		{
			IScraper mailScraper = new EmailScraper(new ChromeDriverFactory(), logger: Log.Logger);

			BlockingCollection<string> domainsToScrap = new BlockingCollection<string>(_webSites.Count);

			Task[] consumers = new Task[6];
			for (int i = 0; i < consumers.Length; i++)
			{
				consumers[i] = Task.Run(async () =>
				{
					foreach (var domain in domainsToScrap.GetConsumingEnumerable())
					{
						var scrapingRes = await mailScraper.Scrape(domain);
						_scrapeResults.Add(scrapingRes);
					}
				});
			}

			Task producer = Task.Run(() =>
			{
				foreach (var domain in _webSites)
				{
					domainsToScrap.Add(domain);
				}

				domainsToScrap.CompleteAdding();
			});

			producer.Wait();
			Task.WaitAll(consumers);

			_scrapeResults.CompleteAdding();
		}

		private static void PrintScrapeResult(Stopwatch stopwatch)
		{
			Console.WriteLine($"Scraping {_webSites.Count} domains took: {stopwatch.Elapsed}");

			foreach (var scrapeResult in _scrapeResults.Take(50))
			{
				Console.WriteLine($"domain: {scrapeResult.Domain} emails: {scrapeResult}");
			}
		}

		private static void LoadDomainsFromFile(string fileName)
		{
			try
			{
				var listAddress = new List<string>();
				var filePath = Path.Combine(Environment.CurrentDirectory, fileName);

				using var reader = File.OpenText(filePath);
				using var csv = new CsvReader(reader, new CsvHelper.Configuration.Configuration() { HasHeaderRecord = false });

				while (csv.Read())
				{
					listAddress.Add(csv.GetField(0));
				}

				_webSites = listAddress;
			}
			catch (Exception e)
			{
				Log.Logger.Log($"Could not read file '{fileName}', {e.Message}", Serilog.Events.LogEventLevel.Error);
				throw;
			}
		}

		private static void InitLogger()
		{
			ILogger logger = null;

			try
			{
				var logsDir = "logs";
				if (!Directory.Exists(logsDir))
				{
					Directory.CreateDirectory(logsDir);
				}

				logger = new LoggerConfiguration()
					.MinimumLevel.Debug()
					.WriteTo.File(
						$"{Path.Combine(logsDir, nameof(WebScraper).ToLower())}.log",
						rollingInterval: RollingInterval.Day,
						rollOnFileSizeLimit: true,
						fileSizeLimitBytes: 16777216)
					.WriteTo.Console()
					.CreateLogger();
			}
			catch (Exception e)
			{
				Console.WriteLine($"Cannot not create logger, {e.Message}");
				throw;
			}

			Log.Logger = logger;
		}
	}
}
