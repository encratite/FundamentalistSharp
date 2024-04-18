using Fundamentalist.Common;
using Fundamentalist.Trainer.Algorithm;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;

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
				// All of these are awful
				/*
				new Sdca(100, null, null),
				// Best?
				new Sdca(1000, null, null),
				new Sdca(10000, null, null),
				new Sdca(100, 0.01f, null),
				new Sdca(1000, 0.01f, null),
				new Sdca(100, null, 0.01f),
				new Sdca(1000, null, 0.01f),
				*/
				// All of these are awful
				/*
				new Lbfgs(1e-7f, 1f, 1f),
				new Lbfgs(1e-6f, 1f, 1f),
				new Lbfgs(1e-5f, 1f, 1f),
				new Lbfgs(1e-4f, 1f, 1f),
				new Lbfgs(1e-7f, 1f, 0f),
				new Lbfgs(1e-7f, 0.5f, 0f),
				// Best?
				new Lbfgs(1e-7f, 0.1f, 0f),
				new Lbfgs(1e-7f, 0f, 1f),
				new Lbfgs(1e-7f, 0f, 0.5f),
				new Lbfgs(1e-7f, 0f, 0.1f),
				*/
				// Useless
				/*
				new Sgd(10, 0.01, 1e-6f),
				new Sgd(100, 0.01, 1e-6f),
				new Sgd(1000, 0.01, 1e-6f),
				new Sgd(10000, 0.01, 1e-6f),
				new Sgd(10000, 0.001, 1e-6f),
				new Sgd(10000, 0.0001, 1e-6f),
				*/
				// There might be some hope for these with high iterations, manual learning rate only makes results worse
				/*
				new LightLgbm(100, null, null, null),
				new LightLgbm(100, 1e-2, null, null),
				new LightLgbm(100, 1e-3, null, null),
				new LightLgbm(500, null, null, null),
				new LightLgbm(1000, null, null, null),
				new LightLgbm(1000, 1e-2, null, null),
				new LightLgbm(1000, 1e-3, null, null),
				new LightLgbm(10000, null, null, null),
				new LightLgbm(10000, 1e-2, null, null),
				new LightLgbm(10000, 1e-3, null, null),
				*/
				new LightLgbm(10000, null, null, null),
				new LightLgbm(50000, null, null, null),
				new LightLgbm(100000, null, null, null),
				/*
				new FastTree(10, 1000, 10, 2),
				new FastTree(10, 2000, 10, 2),
				new FastTree(10, 1000, 10, 5),
				new FastTree(10, 2000, 10, 5),
				new FastTree(10, 1000, 10, 10),
				new FastTree(10, 2000, 10, 10),
				*/
				/*
				new FastTree(10, 1000, 10, 1.1),
				new FastTree(10, 1000, 10, 1.2),
				new FastTree(10, 1000, 10, 1.3),
				new FastTree(10, 1000, 10, 1.4),
				new FastTree(10, 1000, 10, 1.5),
				new FastTree(10, 1000, 10, 1.6),
				// Best?
				new FastTree(10, 1000, 10, 1.7),
				new FastTree(10, 1000, 10, 1.8),
				new FastTree(10, 1000, 10, 1.9),
				new FastTree(10, 1000, 10, 2),
				new FastTree(20, 100, 10, 0.2),
				new FastTree(30, 100, 10, 0.2),
				new FastTree(100, 100, 10, 0.2),
				new FastTree(20, 200, 10, 0.2),
				new FastTree(20, 1000, 10, 0.2),
				new FastTree(20, 100, 10, 0.3),
				new FastTree(20, 100, 10, 1.0),
				new FastTree(20, 100, 20, 0.2),
				new FastTree(20, 100, 50, 0.2),
				new FastTree(20, 100, 100, 0.2),
				*/
				new FastTree(10, 10000, 10, 0.5),
				new FastTree(10, 100000, 10, 0.2),
				new FastTree(10, 100000, 10, 0.1),
				// Completely useless
				// new FastForest(20, 100, 10),
				// new FastForest(30, 100, 10),
				/*
				new FastForest(50, 100, 10),
				new FastForest(100, 100, 10),
				new FastForest(500, 100, 10),
				new FastForest(500, 200, 10),
				new FastForest(500, 500, 10),
				new FastForest(20, 100, 15),
				new FastForest(20, 100, 20),
				new FastForest(20, 100, 50),
				new FastForest(30, 100, 15),
				new FastForest(20, 500, 10),
				new FastForest(20, 1000, 10),
				new FastForest(20, 10000, 10),
				*/
				// Completely useless
				/*
				new Gam(100, 255, 0.002),
				new Gam(200, 255, 0.002),
				new Gam(500, 255, 0.002),
				new Gam(1000, 255, 0.002),
				new Gam(100, 50, 0.002),
				new Gam(100, 100, 0.002),
				new Gam(100, 500, 0.002),
				new Gam(100, 255, 0.005),
				new Gam(100, 255, 0.01),
				new Gam(100, 255, 0.1),
				new Gam(1000, 255, 0.01),
				new Gam(1000, 255, 0.02),
				new Gam(1000, 255, 0.03),
				new Gam(1000, 255, 0.04),
				new Gam(1000, 255, 0.009),
				new Gam(1000, 255, 0.008),
				new Gam(1000, 255, 0.007),
				new Gam(1000, 255, 0.006),
				new Gam(1000, 255, 0.005),
				*/
				/*
				new FieldAwareFactorizationMachine(100, 0.1f, 20),
				new FieldAwareFactorizationMachine(200, 0.1f, 20),
				new FieldAwareFactorizationMachine(500, 0.1f, 20),
				new FieldAwareFactorizationMachine(1000, 0.1f, 20),
				new FieldAwareFactorizationMachine(100, 0.1f, 30),
				new FieldAwareFactorizationMachine(100, 0.1f, 50),
				new FieldAwareFactorizationMachine(1000, 0.09f, 20),
				new FieldAwareFactorizationMachine(1000, 0.08f, 20),
				*/
				/*
				new FieldAwareFactorizationMachine(1000, 0.12f, 22),
				new FieldAwareFactorizationMachine(1000, 0.15f, 25),
				new FieldAwareFactorizationMachine(1000, 0.2f, 30),
				new FieldAwareFactorizationMachine(1000, 0.25f, 35),
				new FieldAwareFactorizationMachine(1000, 0.3f, 50),
				*/
				new FieldAwareFactorizationMachine(10000, 0.1f, 30),
				new FieldAwareFactorizationMachine(50000, 0.1f, 30),
				new FieldAwareFactorizationMachine(100000, 0.1f, 30),
			};
			Backtest backtest = null;
			foreach (var algorithm in algorithms)
			{
				TrainAndEvaluateModel(algorithm);
				/*
				backtest = new Backtest(_testData, _indexPriceData, minimumGain: _options.MinimumGain);
				decimal performance = backtest.Run();
				logPerformance(algorithm.Name, performance);
				*/
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
			Parallel.ForEach(_tickerCache, x =>
			{
				string ticker = x.Key;
				var cacheEntry = x.Value;
				GenerateDataPoints(ticker, cacheEntry, null, _options.TestDate, _trainingData);
				GenerateDataPoints(ticker, cacheEntry, _options.TestDate, null, _testData);
			});
			var sortData = (List<DataPoint> data) => data.Sort((x, y) => x.Date.CompareTo(y.Date));
			sortData(_trainingData);
			sortData(_testData);
			stopwatch.Stop();
			Console.WriteLine($"Removed {_datasetLoader.GoodTickers} tickers ({(decimal)_datasetLoader.BadTickers / (_datasetLoader.BadTickers + _datasetLoader.GoodTickers):P1}) due to insufficient price data");
			Console.WriteLine($"Generated {_trainingData.Count} data points of training data ({GetTrueLabelRatio(_trainingData):P2} true labels) and {_testData.Count} data points of test data ({GetTrueLabelRatio(_testData):P2} true labels) in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private decimal GetTrueLabelRatio(List<DataPoint> dataPoints)
		{
			int trueLabelCount = dataPoints.Where(x => x.Label).Count();
			return (decimal)trueLabelCount / dataPoints.Count;
		}

		private bool InRange(DateTime? date, DateTime? from, DateTime? to)
		{
			return
				(!from.HasValue || date.Value >= from.Value) &&
				(!to.HasValue || date.Value < to.Value);
		}

		private void GenerateDataPoints(string ticker, TickerCacheEntry tickerCacheEntry, DateTime? from, DateTime? to, List<DataPoint> dataPoints)
		{
			var earnings = tickerCacheEntry.Earnings.Where(x => InRange(x.Key, from, to) && x.Key >= _options.TrainingDate).ToList();
			var priceData = tickerCacheEntry.PriceData;
			if (priceData == null)
				return;

			foreach (var x in earnings)
			{
				DateTime date = x.Key;
				var earningsFeatures = x.Value;

				DateTime currentDate = date;
				DateTime futureDate = currentDate + TimeSpan.FromDays(_options.ForecastDays);

				decimal? currentPrice = GetOpenPrice(currentDate, priceData);
				if (!currentPrice.HasValue)
					continue;

				var pastPrices = priceData.Values.Where(x => x.Date < currentDate).Select(x => (float)x.Close).ToArray();
				if (pastPrices.Length < PriceDataMinimum)
					continue;

				var futurePrices = priceData.Values.Where(x => x.Date > currentDate).ToList();
				if (futurePrices.Count < _options.ForecastDays)
					continue;
				decimal futurePrice = futurePrices[_options.ForecastDays - 1].Close;
				decimal gain = futurePrice / currentPrice.Value - 1.0m;

				var priceDataFeatures = TechnicalIndicators.GetFeatures(currentPrice, pastPrices);
				var features = earningsFeatures.Take(_options.Features).Concat(priceDataFeatures);
				bool label = gain > _options.MinimumGain;
				var dataPoint = new DataPoint
				{
					Features = features.ToArray(),
					Label = label,
					// Metadata for backtesting, not used by training
					Ticker = ticker,
					Date = currentDate,
					PriceData = priceData
				};
				lock (dataPoints)
					dataPoints.Add(dataPoint);
			}
		}

		private void TrainAndEvaluateModel(IAlgorithm algorithm)
		{
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

		private void SetScores(IDataView predictions)
		{
			var scores = predictions.GetColumn<float>("Score").ToArray();
			int i = 0;
			foreach (var dataPoint in _testData)
			{
				dataPoint.Score = scores[i];
				i++;
			}
		}
	}
}
