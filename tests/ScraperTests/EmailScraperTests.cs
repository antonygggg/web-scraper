using WebScraper;
using FluentAssertions;
using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScraperTests
{
	/// <summary>
	/// Unit tests class for EmailScraperTests
	/// </summary>
	[TestFixture]
	public class EmailScraperTests
	{
		private IScraper _mailScraper;
		private IScraper _mailScraperZeroTimeout;

		[SetUp]
		public void SetUp()
		{
			_mailScraper = new EmailScraper(new ChromeDriverFactory(), logger: Log.Logger);
			_mailScraperZeroTimeout = new EmailScraper(new ChromeDriverFactory(), logger: Log.Logger, navigationTimeoutMilliSeconds: 0);
		}

		[Test]
		public async Task Scrape_DomainIsNull_ReturnWithStatusNotValidDomain()
		{
			// Arrange
			string domain = null;

			// Action
			var res = await _mailScraper.Scrape(domain);

			// Assertion
			res.Should().NotBeNull();
			res.Status.Should().Be(ScarpingStatus.NotValidDomain);
		}

		[Test]
		public async Task Scrape_DomainIsNewrelicCom_ReturnPrivacyPageEmails()
		{
			// Arrange
			string domain = "newrelic.com";

			// Action
			var res = await _mailScraper.Scrape(domain);

			// Assertion
			res.Should().NotBeNull();
			res.Status.Should().Be(ScarpingStatus.Succesfull);
			res.EmailAddresses.Should().NotBeEmpty();
			foreach (var email in res.EmailAddresses)
			{
				email.FoundInUrl.Should().ContainAll(new string[] { domain, "privacy" });
				email.Occurrences.Should().BeGreaterThan(0);
				email.EmailAddress.Should().EndWithEquivalent($"@{domain}");
			}
		}

		[Test]
		public async Task Scrape_DomainWikipediaOrg_ReturnEmptyEmailsCollection()
		{
			// Arrange
			string domain = "wikipedia.org";

			// Action
			var res = await _mailScraper.Scrape(domain);

			// Assertion
			res.Should().NotBeNull();
			res.Status.Should().Be(ScarpingStatus.Succesfull);
			res.EmailAddresses.Should().BeEmpty();
		}

		[Test]
		public async Task Scrape_DomainIsNewrelicCom_ReturnResultWithTimeout()
		{
			// Arrange
			string domain = "newrelic.com";

			// Action
			var res = await _mailScraperZeroTimeout.Scrape(domain);

			// Assertion
			res.Should().NotBeNull();
			res.Status.Should().Be(ScarpingStatus.Timeout);
			res.Status.Should().NotBeNull();
		}
	}
}
