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
				/*
				new LightLgbm(20, null, 10, 1000),
				new LightLgbm(30, null, 10, 1000),
				new LightLgbm(40, null, 10, 1000),
				new LightLgbm(50, null, 10, 1000),
				new LightLgbm(60, null, 10, 1000),
				new LightLgbm(70, null, 10, 1000),
				new LightLgbm(80, null, 10, 1000),
				new LightLgbm(90, null, 10, 1000),
				new LightLgbm(100, null, 10, 1000),
				*/
				/*
				new LightLgbm(70, null, 2, 1000),
				new LightLgbm(70, null, 4, 1000),
				new LightLgbm(70, null, 6, 1000),
				new LightLgbm(70, null, 8, 1000),
				new LightLgbm(70, null, 10, 1000),
				new LightLgbm(70, null, 12, 1000),
				new LightLgbm(70, null, 14, 1000),
				*/
				/*
				new LightLgbm(70, null, 10, 10),
				new LightLgbm(70, null, 10, 50),
				new LightLgbm(70, null, 10, 100),
				new LightLgbm(70, null, 10, 500),
				*/
				// new LightLgbm(50, null, 10, 100),
				// new LightLgbm(75, null, 10, 100),
				// new LightLgbm(100, null, 10, 100),
				// new LightLgbm(250, null, 10, 100)
				/*
				new LightLgbm(75, null, 20, 100),
				new LightLgbm(75, null, 50, 100),
				new LightLgbm(75, null, 100, 100),
				*/
				// Best for 10000 features?
				// new LightLgbm(75, null, 10, 100)
				/*
				new LightLgbm(10, null, 10, 100),
				new LightLgbm(25, null, 10, 100),
				new LightLgbm(50, null, 10, 100),
				new LightLgbm(75, null, 10, 100),
				new LightLgbm(100, null, 10, 100),
				*/
				/*
				new LightLgbm(250, null, 10, 100),
				new LightLgbm(500, null, 10, 100),
				new LightLgbm(750, null, 10, 100),
				new LightLgbm(1000, null, 10, 100),
				*/
				// new LightLgbm(75, null, 10, 100),
				// new FastTree(false, 20, 100, 10, 0.2),
				/*
				new FastTree(false, 20, 100, 20, 0.2),
				// Best:
				new FastTree(false, 20, 100, 30, 0.2),
				new FastTree(false, 20, 100, 40, 0.2),
				new FastTree(false, 20, 100, 50, 0.2),
				new FastTree(false, 20, 100, 100, 0.2),
				new FastTree(false, 20, 100, 1000, 0.2),
				*/
				// new FastTree(false, 20, 10, 30, 0.2),
				// new FastTree(false, 20, 25, 30, 0.2),
				/*
				new FastTree(false, 20, 50, 30, 0.2),
				new FastTree(false, 20, 150, 30, 0.2),
				new FastTree(false, 20, 500, 30, 0.2),
				new FastTree(false, 20, 1000, 30, 0.2),
				new FastTree(false, 10, 100, 30, 0.2),
				new FastTree(false, 25, 100, 30, 0.2),
				new FastTree(false, 50, 100, 30, 0.2),
				new FastTree(false, 100, 100, 30, 0.2),
				new FastTree(false, 100, 100, 100, 0.2),
				*/
				/*
				new FastTree(false, 10, 100, 30, 0.15),
				new FastTree(false, 10, 100, 30, 0.1),
				new FastTree(false, 10, 100, 30, 0.25),
				new FastTree(false, 10, 100, 30, 0.5),
				new FastTree(false, 10, 100, 30, 0.01),
				new FastTree(false, 10, 200, 30, 0.01),
				new FastTree(false, 10, 300, 30, 0.01),
				new FastTree(false, 10, 400, 30, 0.01),
				new FastTree(false, 10, 500, 30, 0.01),
				*/
				/*
				new FastForest(false, 20, 100, 10),
				new FastForest(false, 30, 100, 10),
				new FastForest(false, 50, 100, 10),
				new FastForest(false, 100, 100, 10),
				new FastForest(false, 500, 100, 10),
				new FastForest(false, 1000, 100, 10),
				new FastForest(false, 20, 50, 10),
				new FastForest(false, 20, 100, 10),
				new FastForest(false, 20, 250, 10),
				new FastForest(false, 20, 500, 10),
				new FastForest(false, 20, 1000, 10),
				new FastForest(false, 20, 500, 10),
				*/
				new Gam(false, 10, 255, 0.002),
				new Gam(false, 25, 255, 0.002),
				new Gam(false, 50, 255, 0.002),
				new Gam(false, 100, 255, 0.002),
				new Gam(false, 10, 500, 0.003),
				new Gam(false, 25, 500, 0.004),
				new Gam(false, 50, 500, 0.005),
				new Gam(false, 100, 500, 0.006),
				new Gam(false, 200, 500, 0.007),
				new Gam(false, 300, 500, 0.008),
				new Gam(false, 400, 500, 0.009),
				new Gam(false, 500, 500, 0.01),
			};
			Backtest backtest = null;
			foreach (var algorithm in algorithms)
			{
				TrainAndEvaluateModel(algorithm);
				/*
				backtest = new Backtest(_testData, _indexPriceData);
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
				_datasetLoader.Load(_earningsPath, _priceDataDirectory, null, PriceDataMinimum, _options.TrainingDate);
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
			decimal trainingOutperform = GetLabelratio(PerformanceLabelType.Outperform, _trainingData);
			decimal trainingUnderperform = GetLabelratio(PerformanceLabelType.Underperform, _trainingData);
			decimal testOutperform = GetLabelratio(PerformanceLabelType.Outperform, _testData);
			decimal testUnderperform = GetLabelratio(PerformanceLabelType.Underperform, _testData);
			Console.WriteLine($"Generated {_trainingData.Count} data points of training data ({trainingOutperform:P2} outperform, {trainingUnderperform:P2} underperform) and {_testData.Count} data points of test data ({testOutperform:P2} outperform, {testUnderperform:P2} underperform) in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private decimal GetLabelratio(PerformanceLabelType label, List<DataPoint> dataPoints)
		{
			int count = dataPoints.Where(x => x.Label == (UInt32)label).Count();
			return (decimal)count / dataPoints.Count;
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

			foreach (var currentEarnings in earnings)
			{
				DateTime currentDate = currentEarnings.Key + TimeSpan.FromDays(_options.ForecastDays);
				if (!IsWorkingDay(currentDate))
					continue;
				DateTime futureDate = GetNextWorkingDay(currentDate + TimeSpan.FromDays(1));
				var earningsFeatures = currentEarnings.Value;

				decimal? currentPrice = GetOpenPrice(currentDate, priceData);
				if (!currentPrice.HasValue)
					continue;
				decimal? futurePrice = GetOpenPrice(futureDate, priceData);
				if (!futurePrice.HasValue)
					continue;
				decimal? currentIndexPrice = GetOpenPrice(currentDate, _indexPriceData);
				if (!currentIndexPrice.HasValue)
					continue;
				decimal? futureIndexPrice = GetOpenPrice(futureDate, _indexPriceData);
				if (!futureIndexPrice.HasValue)
					continue;
				var pastPrices = priceData.Values.Where(x => x.Date < currentDate).Select(x => (float)x.Close).ToArray();
				if (pastPrices.Length < PriceDataMinimum)
					continue;

				decimal performance = futurePrice.Value / currentPrice.Value - futureIndexPrice.Value / currentIndexPrice.Value;
				var generalFeatures = new float[]
				{
					(float)currentDate.DayOfWeek
				};
				var priceDataFeatures = TechnicalIndicators.GetFeatures(currentPrice, pastPrices);
				var features = generalFeatures.Concat(earningsFeatures).Concat(priceDataFeatures);
				PerformanceLabelType label = PerformanceLabelType.Neutral;
				if (performance > _options.OutperformLimit)
					label = PerformanceLabelType.Outperform;
				else if (performance < _options.UnderperformLimit)
					label = PerformanceLabelType.Underperform;
				var dataPoint = new DataPoint
				{
					Features = features.ToArray(),
					Label = (UInt32)label,
					// Metadata for backtesting, not used by training
					Ticker = ticker,
					Date = currentDate,
					PriceData = priceData
				};
				lock (dataPoints)
					dataPoints.Add(dataPoint);
			}
		}

		private bool IsWorkingDay(DateTime date)
		{
			return
				date.DayOfWeek != DayOfWeek.Saturday &&
				date.DayOfWeek != DayOfWeek.Sunday;
		}

		private DateTime GetNextWorkingDay(DateTime date)
		{
			while (!IsWorkingDay(date))
				date += TimeSpan.FromDays(1);
			return date;
		}

		private void TrainAndEvaluateModel(IAlgorithm algorithm)
		{
			var mlContext = new MLContext();
			var schema = SchemaDefinition.Create(typeof(DataPoint));
			int featureCount = _trainingData.First().Features.Length;
			schema[nameof(DataPoint.Features)].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
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
				var metrics = mlContext.MulticlassClassification.Evaluate(predictions);
				Console.WriteLine($"  MacroAccuracy: {metrics.MacroAccuracy:P2}");
				Console.WriteLine($"  MicroAccuracy: {metrics.MicroAccuracy:P2}");
				Console.WriteLine($"  LogLoss: {metrics.LogLoss:F3}");
				Console.WriteLine($"  LogLossReduction: {metrics.LogLossReduction:F3}");
				Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
			}
		}

		private void SetScores(IDataView predictions)
		{
			var predictedLabels = predictions.GetColumn<UInt32>("PredictedLabel").Cast<PerformanceLabelType>().ToArray();
			int i = 0;
			foreach (var dataPoint in _testData)
			{
				dataPoint.PredictedLabel = predictedLabels[i];
				i++;
			}
		}
	}
}