using CsvHelper;
using Fundamentalist.Common.Json.AutoSuggest;
using Fundamentalist.Common.Json.FinancialStatement;
using System.Globalization;
using System.Text.RegularExpressions;

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
			var pattern = new Regex("^10-(Q|K)");
			var isValidSource = (string source) =>
			{
				if (source == null)
					return false;
				return pattern.IsMatch(source);
			};
			// Filter out PROSPECTUS data, retain regular and 405 filings
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

		public static List<PriceData> GetPriceData(CompanyTicker ticker)
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

		private static List<PriceData> ReadPriceData(CsvReader csvReader)
		{
			var priceData = new List<PriceData>();
			csvReader.Read();
			csvReader.ReadHeader();
			var dateTimeNullConverter = new NullConverter<DateTime>();
			var decimalNullConverter = new NullConverter<decimal>();
			var longNullConverter = new NullConverter<long>();
			var getDecimal = (string field) => csvReader.GetField<decimal?>(field, decimalNullConverter);
			while (csvReader.Read())
			{
				var priceDataRow = new PriceData
				{
					Date = csvReader.GetField<DateTime?>("Date", dateTimeNullConverter),
					Open = getDecimal("Open"),
					High = getDecimal("High"),
					Low = getDecimal("Low"),
					Close = getDecimal("Close"),
					AdjustedClose = getDecimal("Adj Close"),
					Volume = csvReader.GetField<long?>("Volume", longNullConverter)
				};
				if (priceDataRow.HasNullValues())
					break;
				if (priceDataRow.Date.HasValue && priceDataRow.Open.HasValue && priceDataRow.Open.Value > 0)
					priceData.Add(priceDataRow);
			}
			priceData.Sort((x, y) => x.Date.Value.CompareTo(y.Date.Value));
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
