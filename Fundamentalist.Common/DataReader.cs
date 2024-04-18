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

		public static ConcurrentBag<EarningsLine> GetEarnings(string csvPath, int? featureLimit, HashSet<int> featureSelection)
		{
			var output = new ConcurrentBag<EarningsLine>();
			var lines = File.ReadAllLines(csvPath);
			Parallel.ForEach(lines.Skip(1), line =>
			{
				var tokens = Split(line);
				var featureTokens = tokens.Skip(2);
				if (featureLimit != null)
					featureTokens = featureTokens.Take(featureLimit.Value);
				float[] features = featureTokens.Select(x => float.Parse(x)).ToArray();
				if (featureSelection != null)
				{
					var filteredFeatures = new float[featureSelection.Count];
					int destinationIndex = 0;
					foreach (int sourceIndex in featureSelection)
					{
						filteredFeatures[destinationIndex] = features[sourceIndex];
						destinationIndex++;
					}
					features = filteredFeatures;
				}
				var earningsLine = new EarningsLine()
				{
					Ticker = tokens[0],
					Date = DateTime.Parse(tokens[1]),
					Features = features
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
				if (
					priceData.Open == 0 ||
					priceData.Close == 0 ||
					priceData.Volume == 0
				)
					continue;
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
