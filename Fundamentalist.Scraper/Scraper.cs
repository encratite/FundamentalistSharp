using Fundamentalist.Common;
using System.Web;

namespace Fundamentalist.Scraper
{
	internal class Scraper
	{
		private HttpClient _httpClient = new HttpClient();

		public void Run()
		{
			DownloadIndex();
			var tickers = GetTickers();
			foreach (var ticker in tickers)
			{
				string secId = GetSecId(ticker);
				if (secId != null)
				{
					DownloadFinancialStatements(secId, ticker);
					DownloadKeyRatios(secId, ticker);
					DownloadPriceData(ticker);
				}
				else
					Utility.WriteError($"Unable to determine sec ID for \"{ticker}\"");
			}
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

		private void DownloadIndex()
		{
			var indexTicker = CompanyTicker.GetIndexTicker();
			DownloadPriceData(indexTicker);
		}

		private List<CompanyTicker> GetTickers()
		{
			const string WilshireStocksUri = "https://raw.githubusercontent.com/derekbanas/Python4Finance/main/Wilshire-5000-Stocks.csv";
			string httpContent = DownloadFile(WilshireStocksUri, Configuration.StocksPath);
			return DataReader.GetTickers(httpContent);
		}

		private string GetSecId(CompanyTicker ticker)
		{
			string encodedCompany = HttpUtility.UrlEncode(ticker.Company);
			string uri = $"https://services.bingapis.com/contentservices-finance.csautosuggest/api/v1/Query?query={encodedCompany}&market=en-us&count=250";
			string path = ticker.GetJsonPath(Configuration.SecIdDirectory);
			string httpContent = DownloadFile(uri, path);
			return DataReader.GetSecId(ticker, httpContent);
		}

		private void DownloadFinancialStatements(string secId, CompanyTicker ticker)
		{
			string encodedSecId = HttpUtility.UrlEncode(secId);
			string uri = $"https://assets.msn.com/service/Finance/Equities/financialstatements?apikey=0QfOX3Vn51YCzitbLaRkTTBadtWpgTN8NZLW0C1SEM&ocid=finance-utils-peregrine&cm=en-us&it=web&scn=ANON&$filter=_p%20eq%20%27{encodedSecId}%27&$top=200&wrapodata=false";
			string path = ticker.GetJsonPath(Configuration.FinancialStatementsDirectory);
			DownloadOnly(uri, path);
		}

		private void DownloadKeyRatios(string secId, CompanyTicker ticker)
		{
			string encodedSecId = HttpUtility.UrlEncode(secId);
			string uri = $"https://services.bingapis.com/contentservices-finance.financedataservice/api/v1/KeyRatios?stockId={encodedSecId}";
			string path = ticker.GetJsonPath(Configuration.KeyRatiosDirectory);
			DownloadOnly(uri, path);
		}

		private void DownloadPriceData(CompanyTicker ticker)
		{
			string encodedTicker = HttpUtility.UrlEncode(ticker.Ticker);
			string uri = $"https://query1.finance.yahoo.com/v7/finance/download/{encodedTicker}?period1=0&period2=2000000000&interval=1d&events=history";
			string path = ticker.GetCsvPath(Configuration.PriceDataDirectory);
			DownloadOnly(uri, path);
		}
	}
}
