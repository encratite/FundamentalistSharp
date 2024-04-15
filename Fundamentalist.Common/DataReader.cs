using System.Collections.Concurrent;

namespace Fundamentalist.Common
{
	public static class DataReader
	{
		public const string IndexTicker = "^GSPC";

		public static List<string> GetTickers(string csvPath)
		{
			var dictionary = new ConcurrentDictionary<string, bool>();
			var lines = File.ReadAllLines(csvPath);
			Parallel.ForEach(lines.Skip(1), line =>
			{
				var tokens = line.Split(',');
				string ticker = tokens[0];
				dictionary[ticker] = true;
			});
			var output = dictionary.Keys.Order().ToList();
			return output;
		}

		public static ConcurrentBag<EarningsLine> GetEarnings(string csvPath)
		{
			var output = new ConcurrentBag<EarningsLine>();
			var lines = File.ReadAllLines(csvPath);
			Parallel.ForEach(lines.Skip(1), line =>
			{
				var tokens = line.Split(',');
				var earningsLine = new EarningsLine()
				{
					Ticker = tokens[0],
					Date = DateTime.Parse(tokens[1]),
					Features = tokens.Skip(2).Select(x => float.Parse(x)).ToArray()
				};
				output.Add(earningsLine);
			});
			return output;
		}

		public static SortedList<DateTime, PriceData> GetPriceData(string ticker, string directory)
		{
			string csvPath = Path.Combine(directory, $"{ticker}.csv");
			if (!File.Exists(csvPath))
				return null;
			var lines = File.ReadAllLines(csvPath);
			var output = new SortedList<DateTime, PriceData>();
			foreach (string line in lines.Skip(1))
			{
				var tokens = line.Split(',');
				if (tokens.Any(x => x == "null"))
					continue;
				var priceData = new PriceData
				{
					Date = DateTime.Parse(tokens[0]),
					Open = decimal.Parse(tokens[1]),
					Close = decimal.Parse(tokens[4]),
					Volume = long.Parse(tokens[6]),
				};
				output.Add(priceData.Date, priceData);
			}
			return output;
		}
	}
}
