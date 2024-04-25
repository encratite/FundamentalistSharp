using Fundamentalist.Common;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Fundamentalist.Correlation
{
	internal class CorrelationAnalyzer
	{
		private int _features;
		private DateTime _fromDate;
		private DateTime _toDate;
		private int _forecastDays;
		private int _earningsCount;
		private int _minimumCount;

		private string _earningsPath;
		private string _priceDataDirectory;
		string _nominalCorrelationOutput;
		string _relativeCorrelationOutput;
		string _presenceOutput;
		string _appearanceOutput;
		string _disappearanceOutput;
		string _featureCountOutput;
		string _weekdayOutput;

		private DatasetLoader _datasetLoader = new DatasetLoader();
		private Dictionary<string, TickerCacheEntry> _cache;
		private List<string> _featureNames;
		private SortedList<DateTime, PriceData> _indexData;
		private FeatureStats[] _stats;
		private List<FeatureCountSample> _featureCounts = new List<FeatureCountSample>();
		private ConcurrentDictionary<DayOfWeek, List<float>> _weekdayPerformance = new ConcurrentDictionary<DayOfWeek, List<float>>();
		private ConcurrentDictionary<int, int> _yearEarnings = new ConcurrentDictionary<int, int>();

		public CorrelationAnalyzer(
			string earningsPath,
			string priceDataDirectory,
			int features,
			int minimumCount,
			DateTime fromDate,
			DateTime toDate,
			int forecastDays,
			string nominalCorrelationOutput,
			string relativeCorrelationOutput,
			string presenceOutput,
			string appearanceOutput,
			string disappearanceOutput,
			string featureCountOutput,
			string weekdayOutput
		)
		{
			_earningsPath = earningsPath;
			_priceDataDirectory = priceDataDirectory;
			_features = features;
			_minimumCount = minimumCount;
			_fromDate = fromDate;
			_toDate = toDate;
			_forecastDays = forecastDays;
			_nominalCorrelationOutput = nominalCorrelationOutput;
			_relativeCorrelationOutput = relativeCorrelationOutput;
			_presenceOutput = presenceOutput;
			_appearanceOutput = appearanceOutput;
			_disappearanceOutput = disappearanceOutput;
			_featureCountOutput = featureCountOutput;
			_weekdayOutput = weekdayOutput;
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
			Console.WriteLine($"Calculating coefficients from {_features} features from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()} with a performance lookahead time of {_forecastDays} working days");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			InitializeFeatureStats();
			SetEarningsCount();
			GatherStats();
			foreach (var stats in _stats)
				stats.SetGains();
			var nominalCorrelationResults = GetCorrelationResults(x => x.NominalObservations);
			var relativeCorrelationResults = GetCorrelationResults(x => x.RelativeObservations);
			stopwatch.Stop();
			Console.WriteLine($"Calculated {nominalCorrelationResults.Count} nominal coefficients and {relativeCorrelationResults.Count} relative coefficients from {_earningsCount} SEC filings in {stopwatch.Elapsed.TotalSeconds:F1} s");
			WriteCorrelationResults(nominalCorrelationResults, _nominalCorrelationOutput);
			WriteCorrelationResults(relativeCorrelationResults, _relativeCorrelationOutput);
			LogAppearanceGains(_presenceOutput, (x) => x.MeanPresenceGain, (x) => x.PresenceGains);
			LogAppearanceGains(_appearanceOutput, (x) => x.MeanAppearanceGain, (x) => x.AppearanceGains);
			LogAppearanceGains(_disappearanceOutput, (x) => x.MeanDisappearanceGain, (x) => x.DisappearanceGains);
			LogFeatureCounts();
			LogWeekdayStats();
		}

		private void InitializeFeatureStats()
		{
			_stats = new FeatureStats[_features];
			for (int i = 0; i < _stats.Length; i++)
				_stats[i] = new FeatureStats($"{_featureNames[i]} ({i})");
		}

		private void SetEarningsCount()
		{
			_earningsCount = 0;
			foreach (var entry in _cache.Values)
			{
				foreach (var pair in entry.Earnings)
				{
					if (pair.Key >= _fromDate && pair.Key < _toDate)
						_earningsCount++;
				}
			}
		}

		private float GetMean(List<float> values)
		{
			return values.Sum() / values.Count;
		}

		private decimal GetMean(List<decimal> values)
		{
			return values.Sum() / values.Count;
		}

		private decimal GetMean(List<long> values)
		{
			return (decimal)values.Sum() / values.Count;
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

		private static PriceData GetLastPriceData(DateTime now, SortedList<DateTime, PriceData> priceData)
		{
			var prices = priceData.Where(p => p.Key <= now).ToList();
			PriceData price = prices
				.OrderByDescending(p => p.Key)
				.Select(p => p.Value)
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
			float change = Math.Max(Math.Min(ratio - 1.0f, Maximum), -Maximum);
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

		private float GetAdjustedChange(decimal? lastPrice, decimal? futurePrice, decimal? lastIndexPrice, decimal? futureIndexPrice)
		{
			float change = GetChange((float)lastPrice.Value, (float)futurePrice.Value);
			float indexChange = GetChange((float)lastIndexPrice.Value, (float)futureIndexPrice.Value);
			float adjustedChange = change - indexChange;
			return adjustedChange;
		}

		private void GatherStats()
		{
			Parallel.ForEach(_cache, x =>
			{
				var entry = x.Value;
				float[] previousFeatures = null;
				foreach (var pair in entry.Earnings)
				{
					var now = pair.Key;
					_yearEarnings.AddOrUpdate(now.Year, 1, (key, x) => x + 1);
					if (now < _fromDate || now >= _toDate)
						continue;
					var features = pair.Value;
					var lastPriceData = GetLastPriceData(now, entry.PriceData);
					decimal? futurePrice = GetFuturePrice(now, _forecastDays, entry.PriceData);
					decimal? lastIndexPrice = GetLastPrice(now, _indexData);
					decimal? futureIndexPrice = GetFuturePrice(now, _forecastDays, _indexData);
					if (lastPriceData != null && futurePrice.HasValue && futureIndexPrice.HasValue)
					{
						float adjustedChange = GetAdjustedChange(lastPriceData.Open, futurePrice, lastIndexPrice, futureIndexPrice);
						AnalyzeCurrentFeatures(features, adjustedChange, lastPriceData);
						AddEarningsDayPerformance(now, adjustedChange);

						if (previousFeatures != null)
							PerformFeatureComparison(previousFeatures, features, adjustedChange);
					}
					previousFeatures = features;
				}
			});
		}

		private void AnalyzeCurrentFeatures(float[] features, float adjustedChange, PriceData lastPriceData)
		{
			int count = 0;
			for (int i = 0; i < _features; i++)
			{
				bool featurePresent = features[i] != 0;
				var featureStats = _stats[i];
				if (featurePresent)
				{
					featureStats.PresenceGains.Add(adjustedChange);
					count++;
					float nominalValue = features[i];
					var observation = new Observation(nominalValue, adjustedChange);
					featureStats.NominalObservations.Add(observation);
				}
			}
			var featureCount = new FeatureCountSample(count, adjustedChange, lastPriceData.Open, lastPriceData.Volume);
			_featureCounts.Add(featureCount);
		}

		private void PerformFeatureComparison(float[] previousFeatures, float[] features, float adjustedChange)
		{
			for (int i = 0; i < _features; i++)
			{
				bool hasPrevious = previousFeatures[i] != 0;
				bool hasCurrent = features[i] != 0;
				var featureStats = _stats[i];
				if (hasPrevious && hasCurrent)
				{
					float featureChange = GetChange(previousFeatures[i], features[i]);
					var observation = new Observation(featureChange, adjustedChange);
					featureStats.RelativeObservations.Add(observation);
				}
				else if (!hasPrevious && hasCurrent)
					featureStats.AppearanceGains.Add(adjustedChange);
				else if (hasPrevious && !hasCurrent)
					featureStats.DisappearanceGains.Add(adjustedChange);
			}
		}

		private void AddEarningsDayPerformance(DateTime now, float adjustedChange)
		{
			DayOfWeek key = now.DayOfWeek;
			if (!_weekdayPerformance.ContainsKey(key))
				_weekdayPerformance[key] = new List<float>();
			_weekdayPerformance[key].Add(adjustedChange);
		}

		private ConcurrentBag<CorrelationResult> GetCorrelationResults(Func<FeatureStats, ConcurrentBag<Observation>> selector)
		{
			var results = new ConcurrentBag<CorrelationResult>();
			Parallel.For(0, _features, i =>
			{
				var featureStats = _stats[i];
				string name = featureStats.Name;
				var observations = selector(_stats[i]).ToArray();
				if (observations.Length < _minimumCount)
					return;
				decimal coefficient = GetSpearmanCoefficient(observations);
				var result = new CorrelationResult(name, coefficient, observations.Length);
				results.Add(result);
			});
			return results;
		}

		private void WriteCorrelationResults(ConcurrentBag<CorrelationResult> relativeCorrelationResults, string path)
		{
			using (var writer = new StreamWriter(path))
			{
				var sortedResults = relativeCorrelationResults.OrderByDescending(x => x.Coefficient);
				foreach (var result in sortedResults)
					writer.WriteLine($"{result.Feature}: {result.Coefficient:F3} ({result.Observations}, {(decimal)result.Observations / _earningsCount:P2})");
			}
		}

		private void LogAppearanceGains(string path, Func<FeatureStats, float?> meanSelector, Func<FeatureStats, ConcurrentBag<float>> bagSelector)
		{
			using (var writer = new StreamWriter(path))
			{
				var sortedResults = _stats.Where(x => meanSelector(x).HasValue && bagSelector(x).Count > _minimumCount).OrderByDescending(meanSelector);
				foreach (var result in sortedResults)
					writer.WriteLine($"{result.Name}: {meanSelector(result):F3} ({bagSelector(result).Count})");
			}
		}

		private void LogFeatureCounts()
		{
			using (var writer = new StreamWriter(_featureCountOutput))
			{
				writer.WriteLine("Count,Samples,Performance,Price,Volume");
				var aggregation = new Dictionary<int, FeatureCountAggregation>();
				foreach (var sample in _featureCounts)
				{
					const int GroupSize = 5;
					int key = (sample.Count / GroupSize) * GroupSize;
					FeatureCountAggregation featureCountAggregation;
					if (!aggregation.TryGetValue(key, out featureCountAggregation))
					{
						featureCountAggregation = new FeatureCountAggregation();
						aggregation[key] = featureCountAggregation;
					}
					featureCountAggregation.Performance.Add(sample.Performance);
					featureCountAggregation.Prices.Add(sample.Price);
					featureCountAggregation.Volumes.Add(sample.Volume);
				}

				foreach (var pair in aggregation.OrderBy(x => x.Key))
				{
					float meanPerformance = GetMean(pair.Value.Performance);
					decimal meanPrice = GetMean(pair.Value.Prices);
					decimal meanVolume = GetMean(pair.Value.Volumes);
					writer.WriteLine($"{pair.Key},{pair.Value.Performance.Count},{meanPerformance:F3},{meanPrice:F2},{(long)meanVolume}");
				}
			}
		}

		private void LogWeekdayStats()
		{
			using (var writer = new StreamWriter(_weekdayOutput))
			{
				writer.WriteLine("Day,Performance");
				foreach (var pair in _weekdayPerformance.OrderBy(x => x.Key))
				{
					float mean = GetMean(pair.Value);
					writer.WriteLine($"{pair.Key},{mean}");
				}
			}
		}
	}
}
