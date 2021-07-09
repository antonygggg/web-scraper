# Email Addresses Web Scraper

## Description

Web scraper for email addresses, based on [Selenium Web Driver](https://www.selenium.dev/documentation/en/webdriver/).

With this scraper, you can scrape email addresses inside websites,
based on references inside the URL that contain common words like **Contact**, **About**, **Privacy** and so.
You can call `Scrape(string URL)` few time with a different URLs on the same Scraper and they will be scraped parallelly.

This can be useful for finding contact addresses for some domains that interest you.


Dependencies for the target environment :

- [.Net Core SDK 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) to run this project
- [Google Chrome](https://www.google.com/chrome/) version 89 ( tested with x64 )


## How to use

There are few ways that you can start with :

- Run `Program` and see the scraping of the websites in the `src/WebScraper/web_sites.csv` file.
- Follow `tests/ScraperTests/EmailScraperTests.cs` to see how to set up and call web scraper on a specific web site.

for example :

```csharp
var mailScraper = new EmailScraper();
var res = await mailScraper.Scrape("wikipedia.org");
```

## Why we need Selenium or any other browser emulator?

Why can we just download a web page content as a string and look with regular expressions for what we want to find?

The main reason for this is that browser emulators make web pages behave differently,
for example, load content that will not be triggered with just `WebClient` or `HttpClient` request, and this content can have important content.

There is also some advantage for using browser emulator.
It can make things a lot easier, they are programmable like a real browser action and can help us find elements we need a lot easier and with better performance,
this is because of the `XPath` navigation instead of regular expressions and others out of the box features.
