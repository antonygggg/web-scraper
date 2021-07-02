using System;
using System.Collections.Generic;
using System.Text;
using OpenQA.Selenium;

namespace WebScraper
{
	/// <summary>
	/// Factory for IWebDriverWithExtentions, to be injected in scraping services
	/// </summary>
	public interface IWebDriverFactory
	{
		/// <summary>
		/// Create an IWebDriver instance
		/// </summary>
		/// <returns>IWebDriver instance</returns>
		IWebDriver Create();
	}
}
