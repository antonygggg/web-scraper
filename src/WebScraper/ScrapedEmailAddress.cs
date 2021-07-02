namespace WebScraper
{
	/// <summary>
	/// Email address that was scraped in a page inside the domain
	/// </summary>
	public class ScrapedEmailAddress
	{
		/// <summary>
		/// The email address, eg: mailbox@domain.com
		/// </summary>
		public string EmailAddress { get; set; }

		/// <summary>
		/// The full URL in which the email address was found, eg: https://domain.com/contact-us
		/// </summary>
		public string FoundInUrl { get; set; }

		/// <summary>
		/// Number of time that this email was found in this address
		/// </summary>
		public int Occurrences { get; set; }

		/// <summary>
		/// String representation
		/// </summary>
		/// <returns>string with EmailAddress, FoundInUrl and Occurrences</returns>
		public override string ToString()
		{
			return $"address: {EmailAddress} url: {FoundInUrl} occurrences: {Occurrences}";
		}
	}
}
