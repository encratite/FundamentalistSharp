using Fundamentalist.Common;
using Fundamentalist.Trainer.Algorithm;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Fundamentalist.Trainer
{
	internal enum EarningsFeatureMode
	{
		Common,
		NominalCorrelation,
		Presence
	}

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

		private HashSet<int> _nominalCorrelationFeatures;
		private HashSet<int> _presenceFeatures;

		public void Run(TrainerOptions options, string earningsPath, string priceDataDirectory)
		{
			_options = options;
			_earningsPath = earningsPath;
			_priceDataDirectory = priceDataDirectory;

			LoadIndex();
			_nominalCorrelationFeatures = LoadFeatureIndices(_options.NominalCorrelationPath, _options.NominalCorrelationLimit);
			_presenceFeatures = LoadFeatureIndices(_options.PresencePath, _options.PresenceLimit);
			GetDataPoints();

			var backtestLog = new List<PerformanceData>();
			var logPerformance = (string description, decimal performance) =>
			{
				var performanceData = new PerformanceData(description, performance);
				backtestLog.Add(performanceData);
			};

			var algorithms = new IAlgorithm[]
			{
				// General models:
				/*
				new LightLgbm(100, null, null, null),
				new FastTree(false, 20, 100, 20, 0.2),
				new FastForest(false, 20, 100, 10),
				new Gam(false, 100, 255, 0.002),
				*/

				/*
				new LightLgbm(100, null, null, null),
				new LightLgbm(100, null, 500, 100),
				new LightLgbm(1000, null, 500, 100),
				*/

				new LightLgbm(1000, null, 100, 100),
				new LightLgbm(1000, null, 200, 100),
				new LightLgbm(1000, null, 300, 100),
				new LightLgbm(1000, null, 400, 100),

				// new Gam(false, 10000, 255, 0.002),

				// Best for common features:
				// new LightLgbm(1000, null, 5000, 1000),

				// Best for nominal correlation features:
				// new LightLgbm(500, null, 250, 100),
			};
			Backtest backtest = null;
			foreach (var algorithm in algorithms)
			{
				TrainAndEvaluateModelForCommonFeatures(algorithm);
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

		private HashSet<int> LoadFeatureIndices(string path, decimal limit)
		{
			var output = new HashSet<int>();
			var lines = File.ReadAllLines(path);
			var pattern = new Regex(@"^.+? \((?<index>\d+)\): (?<correlation>.+?) ");
			foreach (string line in lines)
			{
				var match = pattern.Match(line);
				if (!match.Success)
					throw new Exception("Unable to parse line in nominal correlation features");
				var groups = match.Groups;
				int index = int.Parse(groups["index"].Value);
				decimal correlation = decimal.Parse(groups["correlation"].Value);
				if (Math.Abs(correlation) >= limit)
					output.Add(index);
			}
			return output;
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
				var mode = EarningsFeatureMode.Presence;
				GenerateDataPointsFromEarnings(ticker, cacheEntry, null, _options.TestDate, _trainingData, mode);
				GenerateDataPointsFromEarnings(ticker, cacheEntry, _options.TestDate, null, _testData, mode);
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

		private void GenerateDataPointsFromEarnings(string ticker, TickerCacheEntry tickerCacheEntry, DateTime? from, DateTime? to, List<DataPoint> dataPoints, EarningsFeatureMode mode)
		{
			var earnings = tickerCacheEntry.Earnings.Where(x => InRange(x.Key, from, to) && x.Key >= _options.TrainingDate).ToList();
			var priceData = tickerCacheEntry.PriceData;
			if (priceData == null)
				return;

			foreach (var currentEarnings in earnings)
			{
				DateTime currentDate = currentEarnings.Key + TimeSpan.FromDays(_options.DaysSinceEarnings);
				while (currentDate.DayOfWeek != DayOfWeek.Monday)
					currentDate += TimeSpan.FromDays(1);
				DateTime futureDate = GetNextWorkingDay(currentDate + TimeSpan.FromDays(_options.ForecastDays));
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
				float[] features = null;
				if (mode == EarningsFeatureMode.Common)
					features = earningsFeatures.Take(_options.CommonFeatures).ToArray();
				else if (mode == EarningsFeatureMode.NominalCorrelation)
					features = GetIndexBasedFeatures(earningsFeatures, false, _nominalCorrelationFeatures);
				else if (mode == EarningsFeatureMode.Presence)
					features = GetIndexBasedFeatures(earningsFeatures, true, _presenceFeatures);
				PerformanceLabelType label = PerformanceLabelType.Neutral;
				if (performance > _options.OutperformLimit)
					label = PerformanceLabelType.Outperform;
				else if (performance < _options.UnderperformLimit)
					label = PerformanceLabelType.Underperform;
				var dataPoint = new DataPoint
				{
					Features = features,
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

		private float[] GetIndexBasedFeatures(float[] earningsFeatures, bool sign, HashSet<int> indices)
		{
			float[] features;
			var nominalCorrelationFeatures = new float[indices.Count];
			int offset = 0;
			foreach (int index in indices)
			{
				float feature = earningsFeatures[index];
				if (sign)
					feature = feature != 0 ? 1f : 0f;
				nominalCorrelationFeatures[offset] = feature;
				offset++;
			}
			features = nominalCorrelationFeatures;
			return features;
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

		private void TrainAndEvaluateModelForCommonFeatures(IAlgorithm algorithm)
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