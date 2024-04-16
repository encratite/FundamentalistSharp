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
				var tokens = Split(line);
				string ticker = tokens[0];
				dictionary[ticker] = true;
			});
			var output = dictionary.Keys.Order().ToList();
			return output;
		}

		public static ConcurrentBag<EarningsLine> GetEarnings(string csvPath, int? featureLimit)
		{
			var output = new ConcurrentBag<EarningsLine>();
			var lines = File.ReadAllLines(csvPath);
			Parallel.ForEach(lines.Skip(1), line =>
			{
				var tokens = Split(line);
				var features = tokens.Skip(2);
				if (featureLimit != null)
					features = features.Take(featureLimit.Value);
				var earningsLine = new EarningsLine()
				{
					Ticker = tokens[0],
					Date = DateTime.Parse(tokens[1]),
					Features = features.Select(x => float.Parse(x)).ToArray()
				};
				output.Add(earningsLine);
			});
			return output;
		}

		public static List<string> GetFeatureNames(string csvPath)
		{
			using (var reader = new StreamReader(csvPath))
			{
				string line = reader.ReadLine();
				var tokens = Split(line);
				return tokens.Skip(2).ToList();
			}
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
				var tokens = Split(line);
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

		private static string[] Split(string line)
		{
			var tokens = line.Split(',');
			return tokens;
		}
	}
}
