using Fundamentalist.Common;
using Fundamentalist.Trainer.Algorithm;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Fundamentalist.Trainer
{
	internal class Trainer
	{
		private const int PriceDataMinimum = 200;
		private const bool PrintEvaluation = true;

		private TrainerOptions _options;
		private string _earningsPath;
		private string _priceDataDirectory;

		private SortedList<DateTime, PriceData> _indexPriceData = null;
		private DatasetLoader _datasetLoader = new DatasetLoader();
		private Dictionary<string, TickerCacheEntry> _tickerCache;

		private List<DataPoint> _trainingData;
		private List<DataPoint> _testData;

		public void Run(TrainerOptions options, string earningsPath, string priceDataDirectory)
		{
			_options = options;
			_earningsPath = earningsPath;
			_priceDataDirectory = priceDataDirectory;

			LoadIndex();
			GetDataPoints();

			var backtestLog = new List<PerformanceData>();
			var logPerformance = (string description, decimal performance) =>
			{
				var performanceData = new PerformanceData(description, performance);
				backtestLog.Add(performanceData);
			};

			var algorithms = new IAlgorithm[]
			{
				/*
				new Sdca(500, null, null),
				new Lbfgs(1e-8f, 1f, 1f),
				new Sgd(250, 0.01, 1e-6f),
				new LightLgbm(1000, null, null, null),
				new FastTree(20, 500, 10, 0.15),
				new FastForest(20, 500, 20),
				*/
				// new Gam(10000, 255, 0.05),
				// new Gam(10000, 255, 0.04),
				// new Gam(10000, 255, 0.03),
				// new Gam(10000, 255, 0.02),
				// new Gam(10000, 255, 0.01),
				// new FieldAwareFactorizationMachine(2000, 0.1f, 20),
				// new LightLgbm(10000, null, null, null),
				// new LightLgbm(25000, null, null, 20),
				new LightLgbm(10000, null, null, null),
				new LightLgbm(10000, 1, null, null),
				new LightLgbm(10000, 2, null, null),
				new LightLgbm(10000, 1e-1, null, null),
				new LightLgbm(10000, 1e-2, null, null),
				new LightLgbm(10000, 1e-3, null, null),
				/*
				new LightLgbm(10000, null, null, 10),
				new LightLgbm(10000, null, null, 20),
				new LightLgbm(10000, null, null, 50),
				*/
				// new LightLgbm(10000, null, null, 100),
				// new LightLgbm(10000, null, null, 8),
				// Best
				// new LightLgbm(10000, null, null, 10),
				// new LightLgbm(10000, null, null, 12),
				// new LightLgbm(10000, null, null, 14),
				// new LightLgbm(10000, null, null, 500),
				// new LightLgbm(15000, null, null, 100),
				// new LightLgbm(20000, null, null, 1000),
				// new FastTree(30, 2000, 10, 0.1),
				// new FastTree(30, 1000, 10, 0.1),
			};

			var random = new Random();
			var evaluatedTickers = new HashSet<string>();
			var performances = new Dictionary<IAlgorithm, AlgorithmPerformance>();
			foreach (var algorithm in algorithms)
				performances[algorithm] = new AlgorithmPerformance();

			var hasPriceData = (DateTime startDate, SortedList<DateTime, PriceData> priceData) =>
			{
				for (DateTime i = startDate; i < startDate + TimeSpan.FromDays(10); i += TimeSpan.FromDays(1))
				{
					if (priceData.ContainsKey(i))
						return true;
				}
				return false;
			};
			while (evaluatedTickers.Count < 10)
			{
				int index = random.Next(_tickerCache.Count);
				string ticker = _tickerCache.Keys.ToList()[index];
				if (evaluatedTickers.Contains(ticker))
					continue;

				var priceData = _tickerCache[ticker].PriceData;
				bool hasTrainingData = hasPriceData(_options.TrainingDate, priceData);
				bool hasTestData = hasPriceData(_options.TestDate, priceData);
				if (!hasTrainingData || !hasTestData)
				{
					Console.WriteLine($"Skipping {ticker} due to lack of price data");
					continue;
				}

				foreach (var algorithm in algorithms)
				{
					var algorithmPerformance = performances[algorithm];
					TrainAndEvaluateModel(ticker, algorithm, algorithmPerformance);
					var performanceList = algorithmPerformance.Performances;
					decimal meanPerformance = performanceList.Sum() / performanceList.Count;
					var f1ScoreList = algorithmPerformance.F1Scores;
					double meanF1Score = f1ScoreList.Sum() / f1ScoreList.Count;
					Console.WriteLine($"Total performance of algorithm \"{algorithm.Name}\" after {performanceList.Count} runs: {meanPerformance - 1:+#.00%;-#.00%;+0.00%} (F1 score {meanF1Score:F3})");
				}
				evaluatedTickers.Add(ticker);
			}
		}

		public static decimal? GetOpenPrice(DateTime date, SortedList<DateTime, PriceData> priceData)
		{
			PriceData output;
			if (priceData.TryGetValue(date, out output))
				return output.Open;
			else
				return null;
		}

		public static decimal? GetClosePrice(DateTime date, SortedList<DateTime, PriceData> priceData)
		{
			PriceData output;
			if (priceData.TryGetValue(date, out output))
				return output.Close;
			else
				return null;
		}

		private void LoadIndex()
		{
			if (_indexPriceData == null)
			{
				_indexPriceData = DataReader.GetPriceData(DataReader.IndexTicker, _priceDataDirectory);
			}
		}

		private void GetDataPoints()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			_trainingData = new List<DataPoint>();
			_testData = new List<DataPoint>();
			if (_tickerCache == null)
			{
				Console.WriteLine("Loading datasets");
				_datasetLoader.Load(_earningsPath, _priceDataDirectory, _options.LoaderFeatures ?? _options.Features, PriceDataMinimum);
				_tickerCache = _datasetLoader.Cache;
				stopwatch.Stop();
				Console.WriteLine($"Loaded datasets in {stopwatch.Elapsed.TotalSeconds:F1} s");
			}
			Console.WriteLine("Generating data points");
			stopwatch.Restart();
			int index = 0;
			foreach (var entry in _tickerCache)
			{
				entry.Value.Index = index;
				index++;
			}
			int trainingIndex = 0;
			int testIndex = 0;
			var trainingIndexMap = new Dictionary<DateTime, int>();
			var testIndexMap = new Dictionary<DateTime, int>();
			foreach (var pair in _tickerCache["AAPL"].PriceData)
			{
				var date = pair.Key;
				var priceData = pair.Value;
				if (date >= _options.TrainingDate && date < _options.TestDate)
				{
					trainingIndexMap[date] = trainingIndex;
					trainingIndex++;
				}
				else if (date >= _options.TestDate)
				{
					testIndexMap[date] = testIndex;
					testIndex++;
				}
			}
			GenerateEmptyDataPoints(trainingIndexMap, _trainingData);
			GenerateEmptyDataPoints(testIndexMap, _testData);
			Parallel.ForEach(_tickerCache, x =>
			{
				string ticker = x.Key;
				var cacheEntry = x.Value;
				GenerateDataPoints(ticker, cacheEntry, null, _options.TestDate, _trainingData, trainingIndexMap);
				GenerateDataPoints(ticker, cacheEntry, _options.TestDate, null, _testData, testIndexMap);
			});
			var setVolumeRatios = (List<DataPoint> dataPoints) =>
			{
				Parallel.ForEach(dataPoints, x =>
				{
					var volumeFeatures = x.VolumeFeatures;
					var newVolumeFeatures = new float[volumeFeatures.Length];
					newVolumeFeatures[0] = 1f;
					for (int i = 1; i < volumeFeatures.Length; i++)
						newVolumeFeatures[i] = volumeFeatures[i] / volumeFeatures[i - 1] - 1f;
					x.VolumeFeatures = newVolumeFeatures;
				});
			};
			setVolumeRatios(_trainingData);
			setVolumeRatios(_testData);
			var sortData = (List<DataPoint> data) => data.Sort((x, y) => x.Date.CompareTo(y.Date));
			sortData(_trainingData);
			sortData(_testData);
			stopwatch.Stop();
			Console.WriteLine($"Removed {_datasetLoader.GoodTickers} tickers ({(decimal)_datasetLoader.BadTickers / (_datasetLoader.BadTickers + _datasetLoader.GoodTickers):P1}) due to insufficient price data");
			Console.WriteLine($"Generated {_trainingData.Count} data points of training data and {_testData.Count} data points of test data in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void GenerateEmptyDataPoints(Dictionary<DateTime, int> indexMap, List<DataPoint> data)
		{
			int count = _tickerCache.Count;
			foreach (var date in indexMap.Keys)
			{
				var dataPoint = new DataPoint
				{
					Date = date,
					CloseOpenFeatures = new float[count],
					HighLowFeatures = new float[count],
					VolumeFeatures = new float[count],
					Labels = new bool[count],
					PerformanceRatios = new decimal[count]
				};
				data.Add(dataPoint);
			}
		}

		private bool InRange(DateTime? date, DateTime? from, DateTime? to)
		{
			return
				(!from.HasValue || date.Value >= from.Value) &&
				(!to.HasValue || date.Value < to.Value);
		}

		private void GenerateDataPoints(string ticker, TickerCacheEntry tickerCacheEntry, DateTime? from, DateTime? to, List<DataPoint> dataPoints, Dictionary<DateTime, int> indexMap)
		{
			var prices = tickerCacheEntry.PriceData;
			foreach (var pair in indexMap)
			{
				var date = pair.Key;
				int index = pair.Value;
				PriceData priceData;
				if (!prices.TryGetValue(date, out priceData))
					continue;
				var dataPoint = dataPoints[index];
				int featureIndex = tickerCacheEntry.Index.Value;
				dataPoint.CloseOpenFeatures[featureIndex] = (float)(priceData.Close / priceData.Open - 1);
				dataPoint.HighLowFeatures[featureIndex] = (float)(priceData.High / priceData.Low - 1);
				dataPoint.VolumeFeatures[featureIndex] = (float)priceData.Volume;

				decimal? futurePrice = null;
				int attempts = 0;
				for (DateTime futureDate = date + TimeSpan.FromDays(_options.ForecastDays); attempts < 7 && !futurePrice.HasValue; futureDate += TimeSpan.FromDays(1), attempts++)
					futurePrice = GetClosePrice(futureDate, prices);
				if (!futurePrice.HasValue)
					continue;
				decimal ratio = futurePrice.Value / priceData.Close;
				decimal gain = ratio - 1m;
				bool label = gain > _options.MinimumGain;
				int labelIndex = tickerCacheEntry.Index.Value;
				dataPoint.Labels[labelIndex] = label;
				dataPoint.PerformanceRatios[labelIndex] = ratio;
			}
		}

		private void TrainAndEvaluateModel(string ticker, IAlgorithm algorithm, AlgorithmPerformance algorithmPerformance)
		{
			Console.WriteLine("==========================");
			Console.WriteLine($"Evaluating ticker {ticker}");
			Console.WriteLine("==========================");
			SetLabels(ticker, _trainingData);
			SetLabels(ticker, _testData);
			var mlContext = new MLContext();
			const string FeatureName = "Features";
			var schema = SchemaDefinition.Create(typeof(DataPoint));
			int featureCount = _trainingData.First().CloseOpenFeatures.Length;
			schema["CloseOpenFeatures"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			schema["HighLowFeatures"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			schema["VolumeFeatures"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			var trainingData = mlContext.Data.LoadFromEnumerable(_trainingData, schema);
			var testData = mlContext.Data.LoadFromEnumerable(_testData, schema);
			var algorithmEstimator = algorithm.GetEstimator(mlContext);
			var estimator =
				mlContext.Transforms.Concatenate("Features", "CloseOpenFeatures", "HighLowFeatures", "VolumeFeatures")
				.Append(algorithmEstimator);
			Console.WriteLine($"Training model with algorithm \"{algorithm.Name}\" using {_trainingData.Count} data points with {3 * featureCount} features each");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var model = estimator.Fit(trainingData);
			stopwatch.Stop();
			Console.WriteLine($"Done training model in {stopwatch.Elapsed.TotalSeconds:F1} s, performing test with {_testData.Count} data points ({((decimal)_testData.Count / (_trainingData.Count + _testData.Count)):P2} of total)");
			var predictions = model.Transform(testData);
			SetScores(predictions);
			BinaryClassificationMetrics metrics;
			if (algorithm.Calibrated)
				metrics = mlContext.BinaryClassification.Evaluate(predictions);
			else
				metrics = mlContext.BinaryClassification.EvaluateNonCalibrated(predictions);
			if (PrintEvaluation)
			{
				Console.WriteLine($"  Accuracy: {metrics.Accuracy:P2}");
				Console.WriteLine($"  F1Score: {metrics.F1Score:F3}");
				Console.WriteLine($"  PositivePrecision: {metrics.PositivePrecision:P2}");
				Console.WriteLine($"  NegativePrecision: {metrics.NegativePrecision:P2}");
				Console.WriteLine($"  PositiveRecall: {metrics.PositiveRecall:P2}");
				Console.WriteLine($"  NegativeRecall: {metrics.NegativeRecall:P2}");
				Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
			}
			decimal performance = 1m;
			const decimal Spread = 0.004m;
			int ratioIndex = _tickerCache[ticker].Index.Value;
			foreach (var dataPoint in _testData)
			{
				if (dataPoint.PredictedLabel.Value)
					performance *= dataPoint.PerformanceRatios[ratioIndex] - Spread;
			}
			Console.WriteLine($"Performance: {performance - 1m:P2}");
			algorithmPerformance.Performances.Add(performance);
			algorithmPerformance.F1Scores.Add(metrics.F1Score);
		}

		private void SetLabels(string ticker, List<DataPoint> dataPoints)
		{
			int index = _tickerCache[ticker].Index.Value;
			Parallel.ForEach(dataPoints, dataPoint =>
			{
				dataPoint.Label = dataPoint.Labels[index];
			});
		}

		private void SetScores(IDataView predictions)
		{
			var predictedLabels = predictions.GetColumn<bool>("PredictedLabel").ToArray();
			int i = 0;
			foreach (var dataPoint in _testData)
			{
				dataPoint.PredictedLabel = predictedLabels[i];
				i++;
			}
		}
	}
}
