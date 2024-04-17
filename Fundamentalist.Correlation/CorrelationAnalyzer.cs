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
		private int _forecastDays;
		private string _outputDirectory;

		private string _earningsPath;
		private string _priceDataDirectory;

		private DatasetLoader _datasetLoader = new DatasetLoader();
		private Dictionary<string, TickerCacheEntry> _cache;
		private List<string> _featureNames;

		public CorrelationAnalyzer(
			string earningsPath,
			string priceDataDirectory,
			int features,
			decimal minimumObservationRatio,
			DateTime fromDate,
			int forecastDays,
			string outputDirectory
		)
		{
			_earningsPath = earningsPath;
			_priceDataDirectory = priceDataDirectory;
			_features = features;
			_minimumObservationRatio = minimumObservationRatio;
			_fromDate = fromDate;
			_forecastDays = forecastDays;
			_outputDirectory = outputDirectory;
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
			for (int i = 1; i <= _forecastDays; i++)
				CalculateCoefficients(i);
		}

		private void CalculateCoefficients(int forecastDays)
		{
			Console.WriteLine($"Calculating coefficients from {_features} features with a minimum observation ratio of {_minimumObservationRatio:P1} from {_fromDate.ToShortDateString()} to {DateTime.Now.ToShortDateString()} with a performance lookahead time of {forecastDays} working days");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var observations = new ConcurrentBag<Observation>[_features];
			for (int i = 0; i < observations.Length; i++)
				observations[i] = new ConcurrentBag<Observation>();

			int earningsCount = 0;
			foreach (var entry in _cache.Values)
			{
				foreach (var pair in entry.Earnings)
				{
					if (pair.Key >= _fromDate)
						earningsCount++;
				}
			}

			Parallel.ForEach(_cache.Values, entry =>
			{
				float[] previousFeatures = null;
				decimal? previousPrice = null;
				foreach (var pair in entry.Earnings)
				{
					var now = pair.Key;
					if (now < _fromDate)
						continue;
					var features = pair.Value;
					var prices = entry.PriceData.Where(p => p.Key > now).ToList();
					if (prices.Count < forecastDays)
						continue;
					decimal price = prices
						.OrderBy(p => p.Key)
						.Skip(forecastDays - 1)
						.Select(p => p.Value.Open)
						.FirstOrDefault();

					if (previousFeatures != null && previousPrice.HasValue)
					{
						float yCurrent = GetChange((float)previousPrice.Value, (float)price);
						for (int i = 0; i < _features; i++)
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
			Parallel.For(0, _features, i =>
			{
				string name = _featureNames[i];
				var featureObservations = observations[i].ToArray();
				if ((decimal)featureObservations.Length / earningsCount < _minimumObservationRatio)
					return;
				decimal coefficient = GetSpearmanCoefficient(featureObservations);
				var result = new CorrelationResult(name, coefficient, featureObservations.Length);
				results.Add(result);
			});

			stopwatch.Stop();
			Console.WriteLine($"Calculated {results.Count} coefficients from {earningsCount} SEC filings in {stopwatch.Elapsed.TotalSeconds:F1} s");

			if (!Directory.Exists(_outputDirectory))
				Directory.CreateDirectory(_outputDirectory);
			foreach (var result in results)
			{
				string path = Path.Combine(_outputDirectory, $"{result.Feature}.csv");
				bool newFile = !File.Exists(path);
				using (var writer = new StreamWriter(path, true))
				{
					if (newFile)
						writer.WriteLine("Day,Coefficient");
					writer.WriteLine($"{forecastDays},{result.Coefficient}");
				}
			}
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
