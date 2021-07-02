using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Serilog;

namespace WebScraper
{
	/// <summary>
	/// Service that helps scraping emails from web domains by links text orientation, for example inside pages of that domain that are linked with specific words.
	/// </summary>
	public class EmailScraper : IScraper, IDisposable
	{
		private readonly Regex _emailTextRegex = new Regex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private readonly string[] _wordsToInclude; // add here more text word to include their hrefs
		private readonly int _navigationTimeoutMilliSeconds;
		private readonly int _webDriversCount;
		private readonly string _xPathContainsString;
		private readonly string _xPathContainsEmailString;
		private readonly string _xPathHtml;

		private readonly Channel<IWebDriver> _webDriverWorkers;
		private readonly ILogger _logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="EmailScraper"/> class
		/// </summary>
		/// <param name="webDriverFactory">Factory for the IWebDriver to be used for the Scraper</param>
		/// <param name="wordsToInclude">Words inside a tags to be added to the pages in the search, words like privacy or contact can be added, this collection is case sensitive</param>
		/// <param name="logger">ILogger instance</param>
		/// <param name="webDriversCount">How many instances of IWebDriver to be used, it can be reduced for lower cpu usage</param>
		/// <param name="navigationTimeoutMilliSeconds">How long to wait for fetching of web page until its considered timeout</param>
		public EmailScraper(IWebDriverFactory webDriverFactory = null, string[] wordsToInclude = null, ILogger logger = null, int webDriversCount = 6, int navigationTimeoutMilliSeconds = 20000)
		{
			_webDriversCount = webDriversCount;
			_navigationTimeoutMilliSeconds = navigationTimeoutMilliSeconds;
			_wordsToInclude = wordsToInclude ?? new string[] { "Privacy", "privacy" };
			_logger = logger ?? Log.Logger;
			_xPathContainsString = BuildXpathSearchString(); // contains version, supports XPath 1.0
			_xPathContainsEmailString = "//*[text()[normalize-space() and contains(.,'@')]]"; // contains version, supports XPath 1.0
			_xPathHtml = "/html";
			_webDriverWorkers = CreateWebDriversWorkersCollection(webDriverFactory ?? new ChromeDriverFactory());
		}

		/// <summary>
		/// Scrapes the domain for email addresses
		/// </summary>
		/// <param name="domain">The domain to scrape</param>
		/// <returns>ScrapeResult object with all the emails found in this domain</returns>
		public async Task<ScrapeResult> Scrape(string domain)
		{
			var domainParsed = Utils.BuildUriStringFromUrl(domain, out string searchDomain);

			ScrapeResult res = CreateResult(domain);

			if (!domainParsed)
			{
				_logger.Log($"Domain for scraping {res.Domain} is not a valid domain", Serilog.Events.LogEventLevel.Warning);
				UpdateStatusAndEndingTime(res, ScarpingStatus.NotValidDomain);
				return res;
			}

			res.DomainForSearch = searchDomain;

			Stopwatch sw = Stopwatch.StartNew();

			_logger.Log($"Starting scrape of {res.DomainForSearch} with {_webDriversCount} WebDriver clients", Serilog.Events.LogEventLevel.Debug);

			IWebDriver browser = await _webDriverWorkers.Reader.ReadAsync();

			// get all the pages that contains text from _wordsToInclude
			string[] linksToSearch = null;
			try
			{
				await Task.Run(() =>
				{
					NavigateUntilLoadComplete(browser, res.DomainForSearch);
					linksToSearch = FindPagesByXpath(browser);
				},
				CreateTimeoutCancellationToken());
			}
			catch (OperationCanceledException)
			{
				// navigation to page was canceled by timeout token
				_logger.Log(
					$"Scrape of {res.DomainForSearch} had timeout after {_navigationTimeoutMilliSeconds} ms, no more pages will be fetched in this domain",
					Serilog.Events.LogEventLevel.Warning);
				UpdateStatusAndEndingTime(res, ScarpingStatus.Timeout); // timeout for waiting to get this web page
				return res;
			}
			catch (WebDriverException e)
			{
				// internal timeout to remote WebDriver client
				_logger.Log(
					$"Internal timeout to remote WebDriver client, {e.Message}",
					Serilog.Events.LogEventLevel.Error);
				UpdateStatusAndEndingTime(res, ScarpingStatus.InternalTimeout);
				return res;
			}
			catch (Exception e)
			{
				_logger.Log(
					$"Could not finished successfully scrape of domain '{res.DomainForSearch}'",
					Serilog.Events.LogEventLevel.Error,
					exception: e);
				UpdateStatusAndEndingTime(res, ScarpingStatus.Other);
				return res;
			}
			finally
			{
				// return the web driver to be asked again
				await _webDriverWorkers.Writer.WriteAsync(browser);
			}

			// get all the email addresses from all the pages inside the domain
			await ScrapeLinks(res, linksToSearch);

			UpdateStatusAndEndingTime(res, ScarpingStatus.Succesfull);

			sw.Stop();
			_logger.Log($"Finish successfully scrape of {res.DomainForSearch} in {sw.Elapsed}", Serilog.Events.LogEventLevel.Debug);

			return res;
		}

		/// <summary>
		/// Dispose the instance
		/// </summary>
		public void Dispose()
		{
			// Dispose all the IWebDriver workers
			while (_webDriverWorkers.Reader.TryRead(out IWebDriver webDriver))
			{
				webDriver.Dispose();
			}
		}

		// update the status and ending time of ScrapeResult
		private void UpdateStatusAndEndingTime(ScrapeResult res, ScarpingStatus status)
		{
			res.Status = status;
			res.EndingTime = DateTimeOffset.Now;
		}

		// init default scrape result
		private ScrapeResult CreateResult(string domain)
		{
			return new ScrapeResult
			{
				Domain = domain,
				RelevantPagesWithoutEmails = new List<string>(),
				EmailAddresses = new List<ScrapedEmailAddress>(),
				StatringTime = DateTimeOffset.Now,
				Status = ScarpingStatus.Init
			};
		}

		// search for mail in each relevant page
		private async Task ScrapeLinks(ScrapeResult res, string[] linksToSearch)
		{
			if (linksToSearch.Length == 0)
			{
				_logger.Log($"No links to scrape were found in {res.DomainForSearch}", Serilog.Events.LogEventLevel.Information);
				return;
			}

			var linkToProcess = new BlockingCollection<string>(linksToSearch.Length);

			// collect all the emails scraped from all the scraping tasks for this domain
			var linksToSearchId = new BlockingCollection<string>(linksToSearch.Length);
			Task[] linkConsumers = new Task[4];
			for (int i = 0; i < linkConsumers.Length; i++)
			{
				linkConsumers[i] = Task.Run(async () =>
				{
					foreach (var link in linksToSearchId.GetConsumingEnumerable())
					{
						await ScrapeInnerLink(link, res, res.Domain);
					}
				});
			}

			foreach (var link in linksToSearch)
			{
				linksToSearchId.Add(link);
			}

			linksToSearchId.CompleteAdding();

			await Task.WhenAll(linkConsumers);
		}

		// search for emails inside we page
		private async Task ScrapeInnerLink(string link, ScrapeResult res, string domain)
		{
			// get WebDriver for searching inside a page
			IWebDriver browser = await _webDriverWorkers.Reader.ReadAsync();
			try
			{
				IEnumerable<IGrouping<string, string>> emails = null;
				await Task.Run(() =>
				{
					NavigateUntilLoadComplete(browser, link);
					// domain is for  requirement it will look for email addresses that are related to the domain being scraped,
					// It should collect any such email address.
					emails = FindContentByRegex(browser, domain);
				}, CreateTimeoutCancellationToken());

				if (emails.Any())
				{
					foreach (var emailStats in emails)
					{
						res.EmailAddresses.Add(new ScrapedEmailAddress
						{
							EmailAddress = emailStats.Key,
							FoundInUrl = link,
							Occurrences = emailStats.Count()
						});
					}
				}
				else
				{
					res.RelevantPagesWithoutEmails.Add(link);
					_logger.Log($"Could not find any emails for page link '{link}' in domain '{res.DomainForSearch}'");
				}
			}
			catch (OperationCanceledException)
			{
				// navigation to page was canceled by timeout token
				_logger.Log(
					$"Fetch of {link} had timeout after {_navigationTimeoutMilliSeconds} ms",
					Serilog.Events.LogEventLevel.Warning);
			}
			catch (WebDriverException e)
			{
				// internal timeout to remote WebDriver client
				_logger.Log(
					$"Internal timeout to remote WebDriver client, {e.Message}",
					Serilog.Events.LogEventLevel.Error);
			}
			catch (Exception e)
			{
				_logger.Log($"Could not finished successfully scrape of page link '{link}'", Serilog.Events.LogEventLevel.Error, exception: e);
			}
			finally
			{
				await _webDriverWorkers.Writer.WriteAsync(browser);
			}
		}

		private void NavigateUntilLoadComplete(IWebDriver browser, string url)
		{
			browser.Navigate().GoToUrl(url);

			// waits for the page to load, if this IWebDriver is a IJavaScriptExecutor there is script based waiting,
			// otherwise it will wait for html tag
			var waiter = new WebDriverWait(browser, TimeSpan.FromMilliseconds(_navigationTimeoutMilliSeconds));
			if (browser is IJavaScriptExecutor)
			{
				waiter.Until(b => ((IJavaScriptExecutor)b).ExecuteScript("return document.readyState").Equals("complete"));
			}
			else
			{
				waiter.Until(b => b.FindElements(By.XPath(_xPathHtml)).Count > 0);
			}
		}

		// find elements that their inner text contains one of the words, and they have href attribute with value
		private string[] FindPagesByXpath(IWebDriver browser)
		{
			return browser.FindElements(By.XPath(_xPathContainsString))
					.Select(e => Utils.GetUriStringFromUrlOrEmpty(e.GetAttribute("href"))) // in order to avoid duplication due to query string info
					.Where(uri => !string.IsNullOrEmpty(uri))
					.GroupBy(uri => uri) // remove duplicates
					.Select(g => g.Key)
					.ToArray();
		}

		// find elements that directly contains strings with @, and extract all the mail addresses,
		// not working as well as it should, XPath is not fully supported, can be rewritten and used for better performance
		private IEnumerable<IGrouping<string, string>> FindContentByXPath(IWebDriver browser, string domain, StringComparison comparsion = StringComparison.OrdinalIgnoreCase)
		{
			return browser.FindElements(By.XPath(_xPathContainsEmailString))
				.SelectMany(it => _emailTextRegex.Matches(it.Text).Where(m => m.Success && m.Value.Contains(domain, comparsion)))
				.Select(m => m.Value.ToLower())
				.GroupBy(e => e);
		}

		// find elements that directly contains strings with @, and extract all the mail addresses
		private IEnumerable<IGrouping<string, string>> FindContentByRegex(IWebDriver browser, string domain, StringComparison comparsion = StringComparison.OrdinalIgnoreCase)
		{
			var pageSource = browser.PageSource;
			return _emailTextRegex.Matches(pageSource)
				.Where(m => m.Success && m.Value.Contains(domain, comparsion))
				.Select(m => m.Value.ToLower())
				.GroupBy(e => e);
		}

		// retrieve CancellationToken with default cancellation time
		private CancellationToken CreateTimeoutCancellationToken()
		{
			return new CancellationTokenSource(_navigationTimeoutMilliSeconds).Token;
		}

		// create the WebDriver workers for the searching of the domain, using the instance IWebDriverFactory
		private Channel<IWebDriver> CreateWebDriversWorkersCollection(IWebDriverFactory webDriverFactory)
		{
			var workers = Channel.CreateBounded<IWebDriver>(_webDriversCount);
			for (int i = 0; i < _webDriversCount; i++)
			{
				workers.Writer.TryWrite(webDriverFactory.Create());
			}

			return workers;
		}

		// build the XPath search string for a tags with href with value and inner text that contains words from _wordsToInclude
		private string BuildXpathSearchString()
		{
			return $"//a[string(@href) and ({string.Join(" or ", Array.ConvertAll(Array.FindAll(_wordsToInclude, w => !string.IsNullOrWhiteSpace(w)), w => $"contains(text(), '{w}')"))})]";
		}
	}
}
