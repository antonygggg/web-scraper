using System;
using System.Collections.Generic;
using System.Text;

namespace WebScraper
{
	/// <summary>
	/// Utils class
	/// </summary>
	public static class Utils
	{
		/// <summary>
		/// Build uri from domain
		/// </summary>
		/// <param name="url">domain</param>
		/// <param name="uriString">domain after uri or null</param>
		/// <param name="pathOnly">if true this will take only the route else it will take also the query string</param>
		/// <returns>true if this url is valid</returns>
		public static bool BuildUriStringFromUrl(string url, out string uriString, bool pathOnly = false)
		{
			uriString = null;
			try
			{
				var uri = new UriBuilder(url).Uri;
				uriString = pathOnly ? uri.GetLeftPart(UriPartial.Path) : uri.AbsoluteUri;
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Get url without the query string if exists, i.e https://yahoo.com?q=Hello will return https://yahoo.com
		/// </summary>
		/// <param name="url">url</param>
		/// <param name="pathOnly">if true this will take only the route else it will take also the query string</param>
		/// <returns>formatted url or empty string</returns>
		public static string GetUriStringFromUrlOrEmpty(string url, bool pathOnly = false)
		{
			return BuildUriStringFromUrl(url, out string uriString, pathOnly) ? uriString : string.Empty;
		}
	}
}
