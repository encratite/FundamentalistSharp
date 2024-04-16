using Fundamentalist.Common;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Fundamentalist.Correlation
{
	internal class CorrelationAnalyzer
	{
		private string _earningsPath;
		private string _priceDataDirectory;

		private DatasetLoader _datasetLoader = new DatasetLoader();
		private Dictionary<string, TickerCacheEntry> _cache;
		private List<string> _featureNames;

		public CorrelationAnalyzer(string earningsPath, string priceDataDirectory)
		{
			_earningsPath = earningsPath;
			_priceDataDirectory = priceDataDirectory;
		}

		public void Run()
		{
			LoadDatasets();
			Analyze();
		}

		private void LoadDatasets()
		{
			Console.WriteLine("Loading datasets");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			_datasetLoader.Load(_earningsPath, _priceDataDirectory);
			_cache = _datasetLoader.Cache;
			_featureNames = DataReader.GetFeatureNames(_earningsPath);
			stopwatch.Stop();
			Console.WriteLine($"Loaded datasets in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void Analyze()
		{
			const int Features = 50;
			const int ForecastDays = 20;

			Console.WriteLine("Calculating coefficients");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var xValues = new List<float>[Features];
			for (int i = 0; i < Features; i++)
				xValues[i] = new List<float>();
			var yValues = new List<float>();

			foreach (var entry in _cache.Values)
			{
				foreach (var pair in entry.Earnings)
				{
					var now = pair.Key;
					var features = pair.Value;
					var prices = entry.PriceData.Where(p => p.Key > now).ToList();
					if (prices.Count < ForecastDays)
						continue;
					decimal price = prices
						.OrderBy(p => p.Key)
						.Skip(ForecastDays - 1)
						.Select(p => p.Value.Open)
						.FirstOrDefault();
					float yCurrent = (float)price;
					yValues.Add(yCurrent);
					for (int i = 0; i < Features; i++)
					{
						float xCurrent = features[i];
						xValues[i].Add(xCurrent);
					}
				}
			}

			int[] yRanks = null;
			var y = yValues.ToArray();
			var results = new ConcurrentBag<CorrelationResult>();
			Parallel.For(0, Features, i =>
			{
				var x = xValues[i].ToArray();
				string name = $"{_featureNames[i]} ({i})";
				decimal coefficient = GetSpearmanCoefficient(x, y, ref yRanks);
				var result = new CorrelationResult(name, coefficient);
				results.Add(result);
			});

			stopwatch.Stop();
			Console.WriteLine($"Calculated coefficients in {stopwatch.Elapsed.TotalSeconds:F1} s");

			var sortedResults = results.OrderByDescending(x => x.Coefficient);
			foreach (var result in sortedResults)
				Console.WriteLine($"{result.Feature}: {result.Coefficient:F2}");
		}

		private decimal GetSpearmanCoefficient(float[] x, float[] y, ref int[] yRanks)
		{
			if (x.Length != y.Length)
				throw new Exception("Arrays must be same length");
			decimal n = x.Length;
			var xRanks = GetRanks(x);
			if (yRanks == null)
				yRanks = GetRanks(y);
			decimal squareSum = 0;
			for (int i = 0; i < x.Length; i++)
			{
				decimal difference = xRanks[i] - yRanks[i];
				squareSum += difference * difference;
			}
			decimal coefficient = 1m - 6m * squareSum / n / (n * n - 1m);
			return coefficient;
		}

		private int[] GetRanks(float[] input)
		{
			int i = 1;
			var indexFloats = input.Select(x => new IndexFloat(x, i++)).OrderBy(x => x.Value);
			int[] output = indexFloats.Select(x => x.Index).ToArray();
			return output;
		}
	}
}
