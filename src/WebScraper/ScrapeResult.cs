using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WebScraper
{
	public class ScrapeResult
	{
		/// <summary>
		/// Domain to search inside
		/// </summary>
		public string Domain { get; set; }

		/// <summary>
		/// Domain after added build as uri
		/// </summary>
		public string DomainForSearch { get; set; }

		/// <summary>
		/// Scraping start time
		/// </summary>
		public DateTimeOffset StatringTime { get; set; }

		/// <summary>
		/// Scraping finish time
		/// </summary>
		public DateTimeOffset EndingTime { get; set; }

		/// <summary>
		/// Scraping of the domain status
		/// </summary>
		[JsonIgnore]
		public ScarpingStatus Status { get; set; }


		/// <summary>
		/// Scraping status string
		/// </summary>
		public string StatusString
		{
			get { return Status.ToString(); }
		}

		/// <summary>
		/// Page that has a link with the relevant words, but eventually doesn't contains addresses (this was added for debug generally)
		/// </summary>
		public ICollection<string> RelevantPagesWithoutEmails { get; set; }

		/// <summary>
		/// All the email addresses found in this domain
		/// </summary>
		public ICollection<ScrapedEmailAddress> EmailAddresses { get; set; }

		/// <summary>
		/// ScrapeResult string representation
		/// </summary>
		/// <returns>string representation</returns>
		public override string ToString()
		{
			return string.Join(", ", EmailAddresses?.Select(x => x.ToString()) ?? Enumerable.Empty<string>());
		}
	}
}
