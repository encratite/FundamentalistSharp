using Fundamentalist.Common;
using HtmlAgilityPack;
using System;
using System.Net;
using System.Web;

namespace Fundamentalist.Scraper
{
	internal class Scraper
	{
		private HttpClient _httpClient = new HttpClient();

		public void Run(string tickersPath, string priceDataDirectory, string profileDirectory, string marketCapDirectory)
		{
			var tickers = DataReader.GetTickersFromJson(tickersPath);
			/*
			DownloadPriceData(DataReader.IndexTicker, priceDataDirectory);
			foreach (var ticker in tickers)
				DownloadPriceData(ticker.Symbol, priceDataDirectory);
			*/
			/*
			Parallel.ForEach(tickers, ticker =>
			{
				DownloadProfileData(ticker.Symbol, profileDirectory);
			});
			*/
			DownloadMarketCapData(marketCapDirectory);
		}

		private void DownloadFile(string uri, string path, int? sleepMilliseconds = null, int? expirationDays = null, bool createEmptyFiles = true)
		{
			bool updateFile = false;
			if (File.Exists(path))
			{
				if (expirationDays.HasValue)
				{
					var creationTime = File.GetCreationTime(path);
					var age = DateTime.Now - creationTime;
					if (age > TimeSpan.FromDays(expirationDays.Value))
						updateFile = true;
					else
					{
						Console.WriteLine($"\"{path}\" is still up to date");
						return;
					}
				}
				else
				{
					Console.WriteLine($"\"{path}\" had already been downloaded");
					return;
				}
			}
			try
			{
				string content = _httpClient.GetStringAsync(uri).Result;
				string? directoryPath = Path.GetDirectoryName(path);
				if (directoryPath != null)
					Directory.CreateDirectory(directoryPath);
				File.WriteAllText(path, content);
				if (updateFile)
					Console.WriteLine($"Updated \"{path}\"");
				else
					Console.WriteLine($"Downloaded \"{path}\"");
			}
			catch (AggregateException exception)
			{
				string message = $"Failed to download \"{path}\"";
				var httpException = exception.InnerException as HttpRequestException;
				if (httpException != null)
				{
					Utility.WriteError($"{message} ({httpException.StatusCode})");
					if (createEmptyFiles && httpException.StatusCode == HttpStatusCode.NotFound)
						File.Create(path);
				}
				else
					Utility.WriteError($"{message} ({exception.InnerException})");
			}
			if (sleepMilliseconds.HasValue)
				Thread.Sleep(sleepMilliseconds.Value);
		}

		private void DownloadPriceData(string ticker, string directory)
		{
			string encodedTicker = HttpUtility.UrlEncode(ticker);
			string uri = $"https://query1.finance.yahoo.com/v7/finance/download/{encodedTicker}?period1=0&period2=2000000000&interval=1d&events=history";
			string path = Path.Combine(directory, $"{ticker}.csv");
			DownloadFile(uri, path, 1000, 7, true);
		}

		private void DownloadProfileData(string ticker, string directory)
		{
			string encodedTicker = HttpUtility.UrlEncode(ticker);
			string uri = $"https://eodhd.com/financial-summary/{encodedTicker}.NYSE";
			string path = Path.Combine(directory, $"{ticker}.html");
			DownloadFile(uri, path, 0, null, false);
		}

		private void DownloadMarketCapData(string directory)
		{
			var paths = new List<string>();
			for (int i = 1; i <= 37; i++)
			{
				string uri = $"https://companiesmarketcap.com/usa/largest-companies-in-the-usa-by-market-cap/?page={i}";
				string path = Path.Combine(directory, $"Index-{i}.html");
				DownloadFile(uri, path, 0, null, false);
				paths.Add(path);
			}
			foreach (string path in paths)
			{
				var document = new HtmlDocument();
				document.Load(path);
				var nodes = document.DocumentNode.SelectNodes("//div[@class='name-div']/a[contains(@href, '/marketcap/')]");
				if (nodes == null)
				{
					Utility.WriteError($"Unable to extract market cap links form {path}");
					continue;
				}
				Parallel.ForEach(nodes, node =>
				{
					var symbolDiv = node.SelectSingleNode("div[@class='company-code']");
					if (symbolDiv == null)
					{
						Utility.WriteError($"Unable to determine symbol name {path}");
						return;
					}
					string symbol = symbolDiv.InnerText.Trim();
					string relativePath = node.GetAttributeValue("href", null);
					var baseUri = new Uri("https://companiesmarketcap.com");
					var marketCapUri = new Uri(baseUri, relativePath);
					string symbolPath = Path.Combine(directory, $"{symbol}.html");
					DownloadFile(marketCapUri.ToString(), symbolPath, 0, null, false);
				});
			}
		}
	}
}
