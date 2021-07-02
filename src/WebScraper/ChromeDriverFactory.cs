using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace WebScraper
{
	/// <summary>
	/// Chrome Driver factory for dependency injection
	/// Implemented using ChromeDriver, Chrome 89.0 is needed to use this class
	/// </summary>
	public class ChromeDriverFactory : IWebDriverFactory
	{
		private static readonly string _currentDirectory = Directory.GetCurrentDirectory();
		private static readonly IReadOnlyList<string> _defaultWebDriverArgs = new string[]
		{
			"--disable-extensions",
			"--disable-plugins-discovery",
			"--headless",
			"--disable-dev-shm-usage",
			"--ignore-certificate-errors",
			"--window-size=1920,1200",
			"--user-agent=\"Mozilla/5.0 (Macintosh; Intel Mac OS X 11_2_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.90 Safari/537.36\""
		};

		/// <summary>
		/// Implements the IWebDriverFactory<ChromeDriver> interface
		/// </summary>
		/// <returns>ChromeDriver web driver</returns>
		public IWebDriver Create()
		{
			var defaultOptions = new ChromeOptions();
			defaultOptions.AddArguments(_defaultWebDriverArgs); // behave the same as AddArgument for collections

			return new ChromeDriver(_currentDirectory, defaultOptions);
		}
	}
}
