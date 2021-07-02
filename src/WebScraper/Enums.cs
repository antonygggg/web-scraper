namespace WebScraper
{
	/// <summary>
	/// Enum containing all the scraping results
	/// </summary>
	public enum ScarpingStatus
	{
		/// <summary>
		/// Unknown ( default )
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// The scraping for the domain has started
		/// </summary>
		Init = 1,

		/// <summary>
		/// The scraping for the domain has finished Succesfully
		/// </summary>
		Succesfull = 2,

		/// <summary>
		/// The domain is valid but not found
		/// </summary>
		NotFound = 3,

		/// <summary>
		/// The domain cannot be pares as valid uri
		/// </summary>
		NotValidDomain = 4,

		/// <summary>
		/// The domain request was taking too long
		/// </summary>
		Timeout = 5,

		/// <summary>
		/// Internal timeout to remote WebDriver client
		/// </summary>
		InternalTimeout = 6,

		/// <summary>
		/// Other, usually for exception thrown during the scraping
		/// </summary>
		Other = 7
	}
}
