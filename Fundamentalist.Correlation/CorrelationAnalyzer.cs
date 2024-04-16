using Fundamentalist.Common;
using System.Collections.Concurrent;
using System.Diagnostics;

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
			const int Features = 100;
			const int ForecastDays = 5;
			const int Year = 2015;

			Console.WriteLine("Calculating coefficients");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			int syntheticFeatures = 0;
			var newFeatureNames = new List<string>();
			for (int i = 0; i < Features; i++)
			{
				for (int j = 0; j < Features; j++)
				{
					if (i != j)
					{
						string name = $"{_featureNames[i]} - {_featureNames[j]}";
						newFeatureNames.Add(name);
						syntheticFeatures++;
					}
				}
			}

			var xValues = new List<float>[syntheticFeatures];
			for (int i = 0; i < xValues.Length; i++)
				xValues[i] = new List<float>();
			var yValues = new List<float>();

			_featureNames = newFeatureNames;

			foreach (var entry in _cache.Values)
			{
				float[] previousFeatures = null;
				decimal? previousPrice = null;
				foreach (var pair in entry.Earnings)
				{
					var now = pair.Key;
					if (now.Year < Year)
						continue;
					var features = pair.Value;
					var prices = entry.PriceData.Where(p => p.Key > now).ToList();
					if (prices.Count < ForecastDays)
						continue;
					decimal price = prices
						.OrderBy(p => p.Key)
						.Skip(ForecastDays - 1)
						.Select(p => p.Value.Open)
						.FirstOrDefault();
					if (previousFeatures != null && previousPrice.HasValue)
					{
						float yCurrent = GetChange((float)previousPrice.Value, (float)price);
						yValues.Add(yCurrent);
						int offset = 0;
						for (int i = 0; i < Features; i++)
						{
							for (int j = 0; j < Features; j++)
							{
								if (i != j)
								{
									float xCurrent = GetChange(previousFeatures[i], features[i]) - GetChange(previousFeatures[j], features[j]);
									xValues[offset].Add(xCurrent);
									offset++;
								}
							}
						}
					}
					previousFeatures = features;
					previousPrice = price;
				}
			}

			int[] yRanks = null;
			var y = yValues.ToArray();
			var results = new ConcurrentBag<CorrelationResult>();
			Parallel.For(0, syntheticFeatures, i =>
			{
				var x = xValues[i].ToArray();
				string name = _featureNames[i];
				decimal coefficient = GetSpearmanCoefficient(x, y, ref yRanks);
				var result = new CorrelationResult(name, coefficient);
				results.Add(result);
			});

			stopwatch.Stop();
			Console.WriteLine($"Calculated coefficients in {stopwatch.Elapsed.TotalSeconds:F1} s");

			var sortedResults = results.OrderByDescending(x => x.Coefficient);
			foreach (var result in sortedResults)
				Console.WriteLine($"{result.Feature}: {result.Coefficient:F3}");
		}

		private float GetChange(float previous, float current)
		{
			const float Maximum = 10.0f;
			const float Epsilon = 1e-4f;
			if (Math.Abs(previous) < Epsilon)
				previous = Math.Sign(previous) * Epsilon;
			float ratio = current / previous;
			if (Math.Abs(ratio) > Maximum)
				ratio = Math.Sign(ratio) * Maximum;
			float change = ratio - 1.0f;
			return change;
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
