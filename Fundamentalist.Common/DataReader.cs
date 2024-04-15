using CsvHelper;
using System.Globalization;

namespace Fundamentalist.Common
{
	public static class DataReader
	{
		public const string IndexTicker = "^GSPC";

		public static List<string> GetTickers(string path)
		{
			using (var stringReader = new StreamReader(path))
			{
				using (var csvReader = new CsvReader(stringReader, CultureInfo.InvariantCulture))
				{
					var set = new HashSet<string>();
					csvReader.Read();
					csvReader.ReadHeader();
					while (csvReader.Read())
					{
						string ticker = csvReader.GetField(0);
						set.Add(ticker);
					}
					var output = new List<string>(set.AsEnumerable().Order());
					return output;
				}
			}
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
