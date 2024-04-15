using Fundamentalist.Common;
using System.Web;

namespace Fundamentalist.Scraper
{
	internal class Scraper
	{
		private HttpClient _httpClient = new HttpClient();

		public void Run(string csvPath, string directory)
		{
			DownloadPriceData(DataReader.IndexTicker, directory);
			var tickers = DataReader.GetTickers(csvPath);
			foreach (string ticker in tickers)
				DownloadPriceData(ticker, directory);
		}

		private string DownloadFile(string uri, string path, bool downloadOnly = false)
		{
			string content = null;
			string fullPath = Path.Combine(Configuration.DataDirectory, path);
			if (File.Exists(fullPath))
			{
				if (downloadOnly)
					Console.WriteLine($"\"{fullPath}\" had already been downloaded");
				else
				{
					content = File.ReadAllText(fullPath);
					Console.WriteLine($"Retrieved \"{fullPath}\" from disk");
				}
			}
			else
			{
				try
				{
					content = _httpClient.GetStringAsync(uri).Result;
					string? directoryPath = Path.GetDirectoryName(fullPath);
					if (directoryPath != null)
						Directory.CreateDirectory(directoryPath);
					File.WriteAllText(fullPath, content);
					Console.WriteLine($"Downloaded \"{fullPath}\"");
				}
				catch (AggregateException exception)
				{
					string message = $"Failed to download \"{fullPath}\"";
					var httpException = exception.InnerException as HttpRequestException;
					if (httpException != null)
						Utility.WriteError($"{message} ({httpException.StatusCode})");
					else
						Utility.WriteError($"{message} ({exception.InnerException})");
				}
			}
			return content;
		}

		private void DownloadOnly(string uri, string path)
		{
			DownloadFile(uri, path, true);
		}

		private void DownloadPriceData(string ticker, string directory)
		{
			string encodedTicker = HttpUtility.UrlEncode(ticker);
			string uri = $"https://query1.finance.yahoo.com/v7/finance/download/{encodedTicker}?period1=0&period2=2000000000&interval=1d&events=history";
			string path = Path.Combine(directory, $"{ticker}.csv");
			DownloadOnly(uri, path);
		}
	}
}
