using Fundamentalist.Common;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Fundamentalist.Correlation
{
	enum FeatureSynthesisMode
	{
		Single,
		Addition,
		Subtraction,
		Division1,
		Division2
	}

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
			const int Features = 500;
			const int ForecastDays = 5;
			const decimal MinimumObservationRatio = 0.01m;
			const FeatureSynthesisMode Mode = FeatureSynthesisMode.Division1;
			DateTime fromDate = new DateTime(2018, 1, 1);

			Console.WriteLine($"Calculating coefficients from {Features} features with a minimum observation ratio of {MinimumObservationRatio:P1} from {fromDate} to {DateTime.Now.Year} with a performance lookahead time of {ForecastDays} working days and mode \"{Mode}\"");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var forEachDynamicFeature = (Action<int, int, int> body) =>
			{
				int featureIndex = 0;
				for (int i = 0; i < Features; i++)
				{
					for (int j = 0; j < Features; j++)
					{
						if (
							(Mode == FeatureSynthesisMode.Addition && i < j) ||
							((Mode == FeatureSynthesisMode.Subtraction || Mode == FeatureSynthesisMode.Division1 || Mode == FeatureSynthesisMode.Division2) && i != j)
						)
						{
							body(i, j, featureIndex);
							featureIndex++;
						}

					}
				}
			};

			int dynamicFeatureCount;
			List<string> dynamicFeatureNames;
			if (Mode == FeatureSynthesisMode.Single)
			{
				dynamicFeatureCount = Features;
				dynamicFeatureNames = _featureNames;
			}
			else
			{
				dynamicFeatureNames = new List<string>();
				forEachDynamicFeature((i, j, featureIndex) =>
				{
					string name = null;
					if (Mode == FeatureSynthesisMode.Addition)
						name = $"{_featureNames[i]} + {_featureNames[j]}";
					else if (Mode == FeatureSynthesisMode.Subtraction)
						name = $"{_featureNames[i]} - {_featureNames[j]}";
					else if (Mode == FeatureSynthesisMode.Division1 || Mode == FeatureSynthesisMode.Division2)
						name = $"{_featureNames[i]} / {_featureNames[j]}";
					dynamicFeatureNames.Add(name);
				});
				dynamicFeatureCount = dynamicFeatureNames.Count;
			}

			var observations = new ConcurrentBag<Observation>[dynamicFeatureCount];
			for (int i = 0; i < observations.Length; i++)
				observations[i] = new ConcurrentBag<Observation>();

			_featureNames = dynamicFeatureNames;

			int earningsCount = 0;
			foreach (var entry in _cache.Values)
			{
				foreach (var pair in entry.Earnings)
				{
					if (pair.Key >= fromDate)
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
					if (now < fromDate)
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
						if (Mode == FeatureSynthesisMode.Single)
						{
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
						else
						{
							forEachDynamicFeature((i, j, featureIndex) =>
							{
								if (Mode == FeatureSynthesisMode.Division1)
								{
									if (
										features[i] != 0 &&
										features[j] != 0
									)
									{
										float xCurrent = GetChange(features[j], features[i]);
										var observation = new Observation(xCurrent, yCurrent);
										observations[featureIndex].Add(observation);
									}
								}
								else
								{
									if (
										previousFeatures[i] != 0 &&
										features[i] != 0 &&
										previousFeatures[j] != 0 &&
										features[j] != 0
									)
									{
										float xCurrent = 0f;
										if (Mode == FeatureSynthesisMode.Addition)
											xCurrent = GetChange(previousFeatures[i], features[i]) + GetChange(previousFeatures[j], features[j]);
										else if (Mode == FeatureSynthesisMode.Subtraction)
											xCurrent = GetChange(previousFeatures[i], features[i]) - GetChange(previousFeatures[j], features[j]);
										else if (Mode == FeatureSynthesisMode.Division2)
											xCurrent = GetChange(features[j], features[i]) / GetChange(previousFeatures[j], previousFeatures[i]);
										var observation = new Observation(xCurrent, yCurrent);
										observations[featureIndex].Add(observation);
									}
								}
							});
						}
					}
					previousFeatures = features;
					previousPrice = price;
				}
			});

			var results = new ConcurrentBag<CorrelationResult>();
			Parallel.For(0, dynamicFeatureCount, i =>
			{
				string name = dynamicFeatureNames[i];
				var featureObservations = observations[i].ToArray();
				if ((decimal)featureObservations.Length / earningsCount < MinimumObservationRatio)
					return;
				decimal coefficient = GetSpearmanCoefficient(featureObservations);
				var result = new CorrelationResult(name, coefficient, featureObservations.Length);
				results.Add(result);
			});
			var sortedResults = results.OrderByDescending(x => x.Coefficient).ToList();

			stopwatch.Stop();
			Console.WriteLine($"Calculated {results.Count} coefficients from {earningsCount} SEC filings in {stopwatch.Elapsed.TotalSeconds:F1} s");
			Console.WriteLine(string.Empty);
			foreach (var result in sortedResults)
				Console.WriteLine($"{result.Feature}: {result.Coefficient:F3} ({result.Observations}, {(decimal)result.Observations / earningsCount:P2})");

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
