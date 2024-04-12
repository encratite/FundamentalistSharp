using CsvHelper;
using Fundamentalist.Common.Json.AutoSuggest;
using Fundamentalist.Common.Json.FinancialStatement;
using Fundamentalist.Common.Json.KeyRatios;
using System.Globalization;

namespace Fundamentalist.Common
{
	public static class DataReader
	{
		public static List<CompanyTicker> GetTickers(string httpContentOverride = null)
		{
			string stocksCsv = httpContentOverride;
			if (stocksCsv == null)
				stocksCsv = ReadFile(Configuration.StocksPath);
			using (var stringReader = new StringReader(stocksCsv))
			{
				using (var csvReader = new CsvReader(stringReader, CultureInfo.InvariantCulture))
				{
					var tickers = csvReader.GetRecords<CompanyTicker>().ToList();
					foreach (var ticker in tickers)
						ticker.Company = ticker.Company.Trim();
					return tickers;
				}
			}
		}

		public static string GetSecId(CompanyTicker ticker, string httpContentOverride = null)
		{
			string path = ticker.GetJsonPath(Configuration.SecIdDirectory);
			string json = httpContentOverride;
			if (json == null)
				json = ReadFile(path);
			if (json == null)
				return null;
			var autoSuggest = Utility.Deserialize<AutoSuggest>(json);
			var stocks = autoSuggest.Data.Stocks;
			if (stocks.Length == 0)
				return null;
			string stockJson = stocks[0];
			var autoSuggestStock = Utility.Deserialize<AutoSuggestStock>(stockJson);
			return autoSuggestStock.SecId;
		}

		public static List<FinancialStatement> GetFinancialStatements(CompanyTicker ticker)
		{
			string path = ticker.GetJsonPath(Configuration.FinancialStatementsDirectory);
			string json = ReadFile(path);
			if (json == null)
				return null;
			var financialStatements = Utility.Deserialize<List<FinancialStatement>>(json);
			// 10-Q filings only, no 10-K or 10-K405 ones
			var isValidSource = (string source) =>
			{
				if (source == null)
					return false;
				return source.StartsWith("10-Q");
			};
			// Filter out PROSPECTUS data
			financialStatements = financialStatements
				.Where(f =>
					isValidSource(f.BalanceSheets?.Source) &&
					isValidSource(f.CashFlow?.Source) &&
					isValidSource(f.IncomeStatement?.Source) &&
					f.SourceDate.HasValue
				)
				.OrderBy(f => f.SourceDate)
				.ToList();
			return financialStatements;
		}

		public static KeyRatios GetKeyRatios(CompanyTicker ticker)
		{
			string path = ticker.GetJsonPath(Configuration.KeyRatiosDirectory);
			string json = ReadFile(path);
			if (json == null)
				return null;
			var keyRatios = Utility.Deserialize<KeyRatios>(json);
			keyRatios.CompanyMetrics = keyRatios.CompanyMetrics.Where(m => m.FiscalPeriodType.StartsWith("Q") && m.EndDate.HasValue).OrderBy(m => m.EndDate).ToList();
			return keyRatios;
		}

		public static SortedList<DateTime, PriceData> GetPriceData(CompanyTicker ticker)
		{
			string path = ticker.GetCsvPath(Configuration.PriceDataDirectory);
			string priceDataCsv = ReadFile(path);
			if (priceDataCsv == null)
				return null;
			using (var stringReader = new StringReader(priceDataCsv))
			{
				using (var csvReader = new CsvReader(stringReader, CultureInfo.InvariantCulture))
				{
					var priceData = ReadPriceData(csvReader);
					return priceData;
				}
			}
		}

		private static SortedList<DateTime, PriceData> ReadPriceData(CsvReader csvReader)
		{
			var priceData = new SortedList<DateTime, PriceData>();
			csvReader.Read();
			csvReader.ReadHeader();
			var dateTimeNullConverter = new NullConverter<DateTime>();
			var decimalNullConverter = new NullConverter<decimal>();
			var longNullConverter = new NullConverter<long>();
			var getDecimal = (string field) => csvReader.GetField<decimal?>(field, decimalNullConverter);
			while (csvReader.Read())
			{
				DateTime? date = csvReader.GetField<DateTime?>("Date", dateTimeNullConverter);
				decimal? open = getDecimal("Open");
				// decimal? high = getDecimal("High");
				// decimal? low = getDecimal("Low");
				decimal? close = getDecimal("Close");
				// decimal? adjustedClose = getDecimal("Adj Close");
				long? volume = csvReader.GetField<long?>("Volume", longNullConverter);
				if (date.HasValue && open.HasValue && close.HasValue && volume.HasValue && open > 0 && close > 0)
				{
					var priceDataRow = new PriceData
					{
						Date = date.Value,
						Open = open.Value,
						// High = high.Value,
						// Low = low.Value,
						Close = close.Value,
						// AdjustedClose = adjustedClose.Value,
						Volume = volume.Value
					};
					priceData.Add(date.Value, priceDataRow);
				}
			}
			return priceData;
		}

		private static string ReadFile(string path)
		{
			string fullPath = Path.Combine(Configuration.DataDirectory, path);
			if (!File.Exists(fullPath))
				return null;
			return File.ReadAllText(fullPath);
		}
	}
}
