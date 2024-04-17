using Fundamentalist.Common;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

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
			const int Features = 1000;
			const int ForecastDays = 5;
			const int Year = 2010;

			Console.WriteLine($"Calculating coefficients from {Year} to {DateTime.Now.Year}");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			/*
			int dynamicFeatureCount = 0;
			var newFeatureNames = new List<string>();
			for (int i = 0; i < Features; i++)
			{
				for (int j = 0; j < Features; j++)
				{
					if (i != j)
					{
						string name = $"{_featureNames[i]} - {_featureNames[j]}";
						newFeatureNames.Add(name);
						dynamicFeatureCount++;
					}
				}
			}
			*/
			int dynamicFeatureCount = Features;
			var dynamicFeatureNames = _featureNames;

			var observations = new ConcurrentBag<Observation>[dynamicFeatureCount];
			for (int i = 0; i < observations.Length; i++)
				observations[i] = new ConcurrentBag<Observation>();

			_featureNames = dynamicFeatureNames;

			int earningsCount = 0;
			foreach (var entry in _cache.Values)
				earningsCount += entry.Earnings.Count;

			Parallel.ForEach(_cache.Values, entry =>
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
						/*
						int offset = 0;
						for (int i = 0; i < Features; i++)
						{
							for (int j = 0; j < Features; j++)
							{
								if (i != j)
								{
									float xCurrent = GetChange(previousFeatures[i], features[i]) - GetChange(previousFeatures[j], features[j]);
									var observation = new Observation(xCurrent, yCurrent);
									observations[offset].Add(observation);
									offset++;
								}
							}
						}
						*/
						for (int i = 0; i < Features; i++)
						{
							if (previousFeatures[i] != 0 && features[i] != 0)
							{
								float xCurrent = GetChange(previousFeatures[i], features[i]);
								var observation = new Observation(xCurrent, yCurrent);
								observations[i].Add(observation);
							}
						}
					}
					previousFeatures = features;
					previousPrice = price;
				}
			});

			var results = new ConcurrentBag<CorrelationResult>();
			Parallel.For(0, dynamicFeatureCount, i =>
			{
				string name = _featureNames[i];
				var featureObservations = observations[i];
				if (featureObservations.Count == 0)
					return;
				decimal coefficient = GetSpearmanCoefficient(featureObservations);
				var result = new CorrelationResult(name, coefficient, featureObservations.Count);
				results.Add(result);
			});
			var sortedResults = results.OrderByDescending(x => x.Coefficient);

			stopwatch.Stop();
			Console.WriteLine($"Calculated {results.Count} coefficients from {earningsCount} SEC filings in {stopwatch.Elapsed.TotalSeconds:F1} s");
			Console.WriteLine(string.Empty);
			foreach (var result in sortedResults)
				Console.WriteLine($"{result.Feature}: {result.Coefficient:F3} ({(decimal)result.Observations / earningsCount:P2})");

			results = null;
			_datasetLoader = null;
			_cache = null;
			_featureNames = null;
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

		private decimal GetSpearmanCoefficient(ConcurrentBag<Observation> observations)
		{
			var observationsArray = observations.ToArray();
			decimal n = observations.Count;
			var xRanks = GetRanks(observationsArray, o => o.X);
			var yRanks = GetRanks(observationsArray, o => o.Y);
			decimal squareSum = 0;
			for (int i = 0; i < xRanks.Length; i++)
			{
				decimal difference = xRanks[i] - yRanks[i];
				squareSum += difference * difference;
			}
			decimal coefficient = 1m - 6m * squareSum / n / (n * n - 1m);
			return coefficient;
		}

		private int[] GetRanks(Observation[] observations, Func<Observation, float> select)
		{
			int i = 1;
			var indexFloats = observations.Select(x => new IndexFloat(select(x), i++)).OrderBy(x => x.Value);
			int[] output = indexFloats.Select(x => x.Index).ToArray();
			return output;
		}
	}
}
