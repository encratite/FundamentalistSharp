using CsvHelper;
using System.Globalization;

namespace Fundamentalist.Common
{
	public static class DataReader
	{
		public const string IndexTicker = "^GSPC";

		public static List<string> GetTickers(string path)
		{
			using (var stream = new StreamReader(path))
			{
				using (var csvReader = new CsvReader(stream, CultureInfo.InvariantCulture))
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

		public static void ReadEarnings(string csvPath, Action<string, DateTime, float[]> handleLine)
		{
			using (var stream = new StreamReader(csvPath))
			{
				using (var csvReader = new CsvReader(stream, CultureInfo.InvariantCulture))
				{
					csvReader.Read();
					csvReader.ReadHeader();
					while (csvReader.Read())
					{
						string ticker = csvReader.GetField(0);
						DateTime date = csvReader.GetField<DateTime>(1);
						int offset = 2;
						float[] features = new float[csvReader.ColumnCount - offset];
						for (int i = 0; i < features.Length; i++)
							features[i] = csvReader.GetField<float>(i + offset);
						handleLine(ticker, date, features);
					}
				}
			}
		}

		public static SortedList<DateTime, PriceData> GetPriceData(string ticker, string directory)
		{
			string path = Path.Combine(directory, $"{ticker}.csv");
			if (!File.Exists(path))
				return null;
			using (var stream = new StreamReader(path))
			{
				using (var csvReader = new CsvReader(stream, CultureInfo.InvariantCulture))
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
	}
}
