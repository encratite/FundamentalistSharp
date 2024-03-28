using CsvHelper;
using Fundamentalist.Common.Json.AutoSuggest;
using Fundamentalist.Common.Json.FinancialStatement;
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
			var autoSuggest = JsonHelper.Deserialize<AutoSuggest>(json);
			var stocks = autoSuggest.Data.Stocks;
			if (stocks.Length == 0)
				return null;
			string stockJson = stocks[0];
			var autoSuggestStock = JsonHelper.Deserialize<AutoSuggestStock>(stockJson);
			return autoSuggestStock.SecId;
		}

		public static List<FinancialStatement> GetFinancialStatements(CompanyTicker ticker)
		{
			string path = ticker.GetJsonPath(Configuration.FinancialStatementsDirectory);
			string json = ReadFile(path);
			if (json == null)
				return null;
			var financialStatements = JsonHelper.Deserialize<List<FinancialStatement>>(json);
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

		public static List<PriceData> ReadPriceData(CsvReader csvReader)
		{
			var priceData = new List<PriceData>();
			csvReader.Read();
			csvReader.ReadHeader();
			var dateTimeNullConverter = new NullConverter<DateTime>();
			var decimalNullConverter = new NullConverter<decimal>();
			var longNullConverter = new NullConverter<long>();
			while (csvReader.Read())
			{
				var getDecimal = (string field) => csvReader.GetField<decimal?>(field, decimalNullConverter);
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
				priceData.Add(priceDataRow);
			}
			return priceData;
		}

		public static string ReadFile(string path)
		{
			string fullPath = Path.Combine(Configuration.DataDirectory, path);
			if (!File.Exists(fullPath))
				return null;
			return File.ReadAllText(fullPath);
		}
	}
}
