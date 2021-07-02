using System;
using System.Collections.Generic;
using System.Text;
using WebScraper;
using FluentAssertions;
using NUnit.Framework;

namespace ScraperTests
{
	[TestFixture]
	public class UtilsTests
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void GerUriStringFromUrlOrEmpty_UrlIsValidWithoutHttp_TrueAndAddedHttp()
		{
			// Arrange
			string url = "newrelic.com";
			string formattedUrl;

			// Action
			bool res = Utils.BuildUriStringFromUrl(url, out formattedUrl);

			// Assertion
			res.Should().BeTrue();
			formattedUrl.Should().BeEquivalentTo("http://newrelic.com/");
		}

		[Test]
		public void GerUriStringFromUrlOrEmpty_UrlIsNotValidWithSpaces_TrueAndAddedHttp()
		{
			// Arrange
			string url = "ne wrel ic.com";
			string formattedUrl;

			// Action
			bool res = Utils.BuildUriStringFromUrl(url, out formattedUrl);

			// Assertion
			res.Should().BeFalse();
			formattedUrl.Should().BeNull();
		}

		[Test]
		public void GerUriStringFromUrlOrEmpty_UrlIsValidStripQueryString_UrlWithoutQueryString()
		{
			// Arrange
			string url = "https://www.bing.com/search?q=football&form=PRILHE&httpsmsn=1&msnews=1";

			// Action
			string res = Utils.GetUriStringFromUrlOrEmpty(url, true);

			// Assertion
			res.Should().BeEquivalentTo("https://www.bing.com/search");
		}

		[Test]
		public void GerUriStringFromUrlOrEmpty_UrlIsNotValidWithSpcialChars_EmptyString()
		{
			// Arrange
			string url = "www.^bing()*.com/";

			// Action
			string res = Utils.GetUriStringFromUrlOrEmpty(url, false);

			// Assertion
			res.Should().BeEmpty();
		}
	}
}
