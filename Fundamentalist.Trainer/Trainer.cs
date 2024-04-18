using Fundamentalist.Common;
using Fundamentalist.Trainer.Algorithm;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
				new Gam(500, 255, 0.002),
				new FieldAwareFactorizationMachine(500, 0.1f, 20),
				*/
				new LightLgbm(10000, null, null, null),
				new FastTree(30, 2000, 10, 0.1),
			};
			foreach (var algorithm in algorithms)
				TrainAndEvaluateModel("AAPL", algorithm);
			/*
			Backtest backtest = null;
			foreach (var algorithm in algorithms)
			{
				TrainAndEvaluateModel("AAPL", algorithm);

				backtest = new Backtest(_testData, _indexPriceData);
				decimal performance = backtest.Run();
				logPerformance(algorithm.Name, performance);
			}

			if (backtest != null)
			{
				logPerformance("S&P 500", backtest.IndexPerformance);
				Console.WriteLine("Options used:");
				_options.Print();
				Console.WriteLine("Performance summary:");
				int maxPadding = backtestLog.MaxBy(x => x.Description.Length).Description.Length;
				foreach (var entry in backtestLog)
					Console.WriteLine($"  {entry.Description.PadRight(maxPadding)} {entry.Performance:#.00%}");
			}
			*/
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
			var sortData = (List<DataPoint> data) => data.Sort((x, y) => x.Date.CompareTo(y.Date));
			sortData(_trainingData);
			sortData(_testData);
			stopwatch.Stop();
			Console.WriteLine($"Removed {_datasetLoader.GoodTickers} tickers ({(decimal)_datasetLoader.BadTickers / (_datasetLoader.BadTickers + _datasetLoader.GoodTickers):P1}) due to insufficient price data");
			Console.WriteLine($"Generated {_trainingData.Count} data points of training data and {_testData.Count} data points of test data in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void GenerateEmptyDataPoints(Dictionary<DateTime, int> indexMap, List<DataPoint> data)
		{
			foreach (var date in indexMap.Keys)
			{
				var dataPoint = new DataPoint
				{
					Date = date,
					Features = new float[_tickerCache.Count],
					Labels = new bool[_tickerCache.Count]
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
			foreach (var pair in indexMap)
			{
				var date = pair.Key;
				int index = pair.Value;
				decimal? price = GetClosePrice(date, tickerCacheEntry.PriceData);
				if (!price.HasValue)
					continue;
				var dataPoint = dataPoints[index];
				dataPoint.Features[tickerCacheEntry.Index.Value] = (float)price.Value;

				decimal? futurePrice = null;
				int attempts = 0;
				for (DateTime futureDate = date + TimeSpan.FromDays(_options.ForecastDays); attempts < 7 && !futurePrice.HasValue; futureDate += TimeSpan.FromDays(1), attempts++)
					futurePrice = GetClosePrice(futureDate, tickerCacheEntry.PriceData);
				if (!futurePrice.HasValue)
					continue;
				decimal gain = futurePrice.Value / price.Value - 1m;
				bool label = gain > _options.MinimumGain;
				dataPoint.Labels[tickerCacheEntry.Index.Value] = label;
			}
		}

		private void TrainAndEvaluateModel(string ticker, IAlgorithm algorithm)
		{
			SetLabels(ticker, _trainingData);
			SetLabels(ticker, _testData);
			var mlContext = new MLContext();
			const string FeatureName = "Features";
			var schema = SchemaDefinition.Create(typeof(DataPoint));
			int featureCount = _trainingData.First().Features.Length;
			schema[FeatureName].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			var trainingData = mlContext.Data.LoadFromEnumerable(_trainingData, schema);
			var testData = mlContext.Data.LoadFromEnumerable(_testData, schema);
			var estimator = algorithm.GetEstimator(mlContext);
			Console.WriteLine($"Training model with algorithm \"{algorithm.Name}\" using {_trainingData.Count} data points with {featureCount} features each");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var model = estimator.Fit(trainingData);
			stopwatch.Stop();
			Console.WriteLine($"Done training model in {stopwatch.Elapsed.TotalSeconds:F1} s, performing test with {_testData.Count} data points ({((decimal)_testData.Count / (_trainingData.Count + _testData.Count)):P2} of total)");
			var predictions = model.Transform(testData);
			SetScores(predictions);
			if (PrintEvaluation)
			{
				BinaryClassificationMetrics metrics;
				if (algorithm.Calibrated)
					metrics = mlContext.BinaryClassification.Evaluate(predictions);
				else
					metrics = mlContext.BinaryClassification.EvaluateNonCalibrated(predictions);
				Console.WriteLine($"  Accuracy: {metrics.Accuracy:P2}");
				Console.WriteLine($"  F1Score: {metrics.F1Score:F3}");
				Console.WriteLine($"  PositivePrecision: {metrics.PositivePrecision:P2}");
				Console.WriteLine($"  NegativePrecision: {metrics.NegativePrecision:P2}");
				Console.WriteLine($"  PositiveRecall: {metrics.PositiveRecall:P2}");
				Console.WriteLine($"  NegativeRecall: {metrics.NegativeRecall:P2}");
				Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
			}
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
