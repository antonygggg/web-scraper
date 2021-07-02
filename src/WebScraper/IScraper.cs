using System.Threading.Tasks;

namespace WebScraper
{
	/// <summary>
	/// Scraper general interface
	/// </summary>
	public interface IScraper
	{
		/// <summary>
		/// Scrapes the domain for email addresses
		/// </summary>
		/// <param name="domain">The domain to scrape</param>
		/// <returns>Scraping result for this domain</returns>
		Task<ScrapeResult> Scrape(string domain);
	}
}
