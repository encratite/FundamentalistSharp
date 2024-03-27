using CSVFile;
using Fundamentalist.Scraper.Json.AutoSuggest;
using Fundamentalist.Scraper.Json.FinancialStatement;
using System.Text.Json;
using System.Web;

namespace Fundamentalist.Scraper
{
	internal class Scraper
	{
		private const string DataDirectory = "Data";
		private const string SecIdDirectory = "SecId";
		private const string FinancialStatementDirectory = "FinancialStatement";

		private HttpClient _httpClient = new HttpClient();

		public void Run()
		{
			var tickers = GetTickers();
			foreach (var ticker in tickers)
			{
				string secId = GetSecId(ticker);
				if (secId != null)
					GetFinancialStatements(secId, ticker);
				else
					WriteError($"Unable to determine sec ID for \"{ticker}\"");
			}
		}

		private string DownloadFile(string uri, string path)
		{
			string content = null;
			string fullPath = Path.Combine(DataDirectory, path);
			if (File.Exists(fullPath))
			{
				content = File.ReadAllText(fullPath);
				Console.WriteLine($"Retrieved \"{fullPath}\" from disk");
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
						WriteError($"{message} ({httpException.StatusCode})");
					else
						WriteError($"{message} ({exception.InnerException})");
				}
			}
			return content;
		}

		private List<CompanyTicker> GetTickers()
		{
			const string WilshireStocksUri = "https://raw.githubusercontent.com/derekbanas/Python4Finance/main/Wilshire-5000-Stocks.csv";
			string stocksCsv = DownloadFile(WilshireStocksUri, "Stocks.csv");
			var tickers = CSV.Deserialize<CompanyTicker>(stocksCsv).ToList();
			foreach (var ticker in tickers)
				ticker.Company = ticker.Company.Trim();
			return tickers;
		}

		private string GetSecId(CompanyTicker ticker)
		{
			string encodedCompany = HttpUtility.UrlEncode(ticker.Company);
			string uri = $"https://services.bingapis.com/contentservices-finance.csautosuggest/api/v1/Query?query={encodedCompany}&market=en-us&count=250";
			string path = ticker.GetJsonPath(SecIdDirectory);
			string json = DownloadFile(uri, path);
			if (json == null)
				return null;
			var autoSuggest = Deserialize<AutoSuggest>(json);
			var stocks = autoSuggest.Data.Stocks;
			if (stocks.Length == 0)
				return null;
			string stockJson = stocks[0];
			var autoSuggestStock = Deserialize<AutoSuggestStock>(stockJson);
			return autoSuggestStock.SecId;
		}

		private FinancialStatement[] GetFinancialStatements(string secId, CompanyTicker ticker)
		{
			string encodedSecId = HttpUtility.UrlEncode(secId);
			string uri = $"https://assets.msn.com/service/Finance/Equities/financialstatements?apikey=0QfOX3Vn51YCzitbLaRkTTBadtWpgTN8NZLW0C1SEM&ocid=finance-utils-peregrine&cm=en-us&it=web&scn=ANON&$filter=_p%20eq%20%27{encodedSecId}%27&$top=200&wrapodata=false";
			string path = ticker.GetJsonPath(FinancialStatementDirectory);
			string json = DownloadFile(uri, path);
			if (json == null)
				return null;
			var financialStatements = Deserialize<FinancialStatement[]>(json);
			return financialStatements;

		}

		private T Deserialize<T>(string json)
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var output = JsonSerializer.Deserialize<T>(json, options);
			return output;
		}

		private void WriteError(string message)
		{
			var previousColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ForegroundColor = previousColor;
		}

	}
}
