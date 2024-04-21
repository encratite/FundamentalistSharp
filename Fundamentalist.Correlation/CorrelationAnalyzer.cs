using Fundamentalist.Common;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Fundamentalist.Correlation
{
	internal class CorrelationAnalyzer
	{
		private int _features;
		private decimal _minimumObservationRatio;
		private DateTime _fromDate;
		private DateTime _toDate;
		private int _forecastDays;
		private int _earningsCount;
		private int _minimumAppearanceCount = 100;

		private string _earningsPath;
		private string _priceDataDirectory;
		string _correlationOutput;
		string _appearanceOutput;
		string _disappearanceOutput;

		private DatasetLoader _datasetLoader = new DatasetLoader();
		private Dictionary<string, TickerCacheEntry> _cache;
		private List<string> _featureNames;
		private SortedList<DateTime, PriceData> _indexData;
		private FeatureStats[] _stats;

		public CorrelationAnalyzer(
			string earningsPath,
			string priceDataDirectory,
			int features,
			decimal minimumObservationRatio,
			DateTime fromDate,
			DateTime toDate,
			int forecastDays,
			string correlationOutput,
			string appearanceOutput,
			string disappearanceOutput
		)
		{
			_earningsPath = earningsPath;
			_priceDataDirectory = priceDataDirectory;
			_features = features;
			_minimumObservationRatio = minimumObservationRatio;
			_fromDate = fromDate;
			_toDate = toDate;
			_forecastDays = forecastDays;
			_correlationOutput = correlationOutput;
			_appearanceOutput = appearanceOutput;
			_disappearanceOutput = disappearanceOutput;
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
			_datasetLoader.Load(_earningsPath, _priceDataDirectory, _features, 200, _fromDate, _toDate);
			_cache = _datasetLoader.Cache;
			_featureNames = DataReader.GetFeatureNames(_earningsPath);
			_indexData = DataReader.GetPriceData(DataReader.IndexTicker, _priceDataDirectory);
			stopwatch.Stop();
			Console.WriteLine($"Loaded datasets in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void Analyze()
		{
			CalculateCoefficients();
		}

		private void CalculateCoefficients()
		{
			Console.WriteLine($"Calculating coefficients from {_features} features with a minimum observation ratio of {_minimumObservationRatio:P1} from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()} with a performance lookahead time of {_forecastDays} working days");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			_stats = new FeatureStats[_features];
			for (int i = 0; i < _stats.Length; i++)
				_stats[i] = new FeatureStats($"{_featureNames[i]} ({i})");

			_earningsCount = 0;
			foreach (var entry in _cache.Values)
			{
				foreach (var pair in entry.Earnings)
				{
					if (pair.Key >= _fromDate && pair.Key < _toDate)
						_earningsCount++;
				}
			}

			GatherStats();
			foreach (var stats in _stats)
				stats.SetGains();
			var correlationResults = GetCorrelationResults(_earningsCount);

			stopwatch.Stop();
			Console.WriteLine($"Calculated {correlationResults.Count} coefficients from {_earningsCount} SEC filings in {stopwatch.Elapsed.TotalSeconds:F1} s");

			using (var writer = new StreamWriter(_correlationOutput))
			{
				var sortedResults = correlationResults.OrderByDescending(x => x.Coefficient);
				foreach (var result in sortedResults)
					writer.WriteLine($"{result.Feature}: {result.Coefficient:F3} ({result.Observations}, {(decimal)result.Observations / _earningsCount:P2})");
			}

			LogAppearanceGains(_appearanceOutput, (x) => x.MeanAppearanceGain, (x) => x.AppearanceGains);
			LogAppearanceGains(_disappearanceOutput, (x) => x.MeanDisappearanceGain, (x) => x.DisappearanceGains);
		}

		private void LogAppearanceGains(string path, Func<FeatureStats, float?> meanSelector, Func<FeatureStats, ConcurrentBag<float>> bagSelector)
		{
			using (var writer = new StreamWriter(path))
			{
				var sortedResults = _stats.Where(x => meanSelector(x).HasValue && bagSelector(x).Count > _minimumAppearanceCount).OrderByDescending(meanSelector);
				foreach (var result in sortedResults)
					writer.WriteLine($"{result.Name}: {meanSelector(result):F3} ({bagSelector(result).Count})");
			}
		}

		private void GatherStats()
		{
			Parallel.ForEach(_cache.Values, entry =>
			{
				float[] previousFeatures = null;
				foreach (var pair in entry.Earnings)
				{
					var now = pair.Key;
					if (now < _fromDate || now >= _toDate)
						continue;
					var features = pair.Value;
					if (previousFeatures != null)
					{
						decimal? lastPrice = GetLastPrice(now, entry.PriceData);
						decimal? futurePrice = GetFuturePrice(now, _forecastDays, entry.PriceData);
						if (!lastPrice.HasValue || !futurePrice.HasValue)
							continue;
						decimal? lastIndexPrice = GetLastPrice(now, _indexData);
						decimal? futureIndexPrice = GetFuturePrice(now, _forecastDays, _indexData);
						if (!lastIndexPrice.HasValue || !futureIndexPrice.HasValue)
							continue;
						float change = GetChange((float)lastPrice.Value, (float)futurePrice.Value);
						float indexChange = GetChange((float)lastIndexPrice.Value, (float)futureIndexPrice.Value);
						float adjustedChange = change - indexChange;
						for (int i = 0; i < _features; i++)
						{
							bool hasPrevious = previousFeatures[i] != 0;
							bool hasCurrent = features[i] != 0;
							var featureStats = _stats[i];
							if (hasPrevious && hasCurrent)
							{
								float xCurrent = GetChange(previousFeatures[i], features[i]);
								var observation = new Observation(xCurrent, adjustedChange);
								featureStats.Observations.Add(observation);
							}
							else if (!hasPrevious && hasCurrent)
								featureStats.AppearanceGains.Add(adjustedChange);
							else if (hasPrevious && !hasCurrent)
								featureStats.DisappearanceGains.Add(adjustedChange);
						}
					}
					previousFeatures = features;
				}
			});
		}

		private ConcurrentBag<CorrelationResult> GetCorrelationResults(int earningsCount)
		{
			var results = new ConcurrentBag<CorrelationResult>();
			Parallel.For(0, _features, i =>
			{
				var featureStats = _stats[i];
				string name = featureStats.Name;
				var observations = _stats[i].Observations.ToArray();
				if ((decimal)observations.Length / earningsCount < _minimumObservationRatio)
					return;
				decimal coefficient = GetSpearmanCoefficient(observations);
				var result = new CorrelationResult(name, coefficient, observations.Length);
				results.Add(result);
			});
			return results;
		}

		private static decimal? GetLastPrice(DateTime now, SortedList<DateTime, PriceData> priceData)
		{
			var prices = priceData.Where(p => p.Key <= now).ToList();
			decimal price = prices
				.OrderByDescending(p => p.Key)
				.Select(p => p.Value.Open)
				.FirstOrDefault();
			return price;
		}

		private static decimal? GetFuturePrice(DateTime now, int forecastDays, SortedList<DateTime, PriceData> priceData)
		{
			var prices = priceData.Where(p => p.Key > now).ToList();
			if (prices.Count < forecastDays)
				return null;
			decimal price = prices
				.OrderBy(p => p.Key)
				.Skip(forecastDays - 1)
				.Select(p => p.Value.Open)
				.FirstOrDefault();
			return price;
		}

		private float GetChange(float previous, float current)
		{
			const float Maximum = 10.0f;
			const float Epsilon = 1e-4f;
			if (previous == 0)
				return 0;
			else if (Math.Abs(previous) < Epsilon)
				previous = Math.Sign(previous) * Epsilon;
			float ratio = current / previous;
			float change = Math.Max(Math.Min(ratio - 1.0f, Maximum), - Maximum);
			return change;
		}

		private decimal GetSpearmanCoefficient(Observation[] observations)
		{
			decimal n = observations.Length;
			var xRanks = GetRanks(observations, o => o.X);
			var yRanks = GetRanks(observations, o => o.Y);
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
