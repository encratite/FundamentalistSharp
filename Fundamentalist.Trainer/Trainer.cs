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

		private List<DataPoint> _commonFeaturesTrainingData;
		private List<DataPoint> _commonFeaturesTestData;
		private List<DataPoint> _priceDataTrainingData;
		private List<DataPoint> _priceDataTestData;

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

			var commonFeaturesAlgorithm = new LightLgbm(1000, null, 5000, 1000);
			var priceDataAlgorithm = new LightLgbm(1000, null, 300, 100);
			var metaAlgorithm = new LightLgbm(1000, null, 250, 100);
			var commonFeaturesPredictions = TrainAndEvaluateModel(_commonFeaturesTrainingData, _commonFeaturesTestData, commonFeaturesAlgorithm);
			var priceDataPredictions = TrainAndEvaluateModel(_priceDataTrainingData, _priceDataTestData, priceDataAlgorithm);
			var metaTrainingData = CreateMetaDataPoints(_commonFeaturesTrainingData, commonFeaturesPredictions.TrainingPredictions);
			var metaTestData = CreateMetaDataPoints(_commonFeaturesTestData, commonFeaturesPredictions.TestPredictions);
			MergeMetaDataPoints(_priceDataTrainingData, priceDataPredictions.TrainingPredictions, metaTrainingData);
			MergeMetaDataPoints(_priceDataTestData, priceDataPredictions.TestPredictions, metaTestData);
			TrainAndEvaluateModel(metaTrainingData, metaTestData, metaAlgorithm);

			FreeData();
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

		private void FreeData()
		{
			_commonFeaturesTrainingData = null;
			_commonFeaturesTestData = null;
			_priceDataTrainingData = null;
			_priceDataTestData = null;
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
			_commonFeaturesTrainingData = new List<DataPoint>();
			_commonFeaturesTestData = new List<DataPoint>();
			_priceDataTrainingData = new List<DataPoint>();
			_priceDataTestData = new List<DataPoint>();
			Parallel.ForEach(_tickerCache, x =>
			{
				string ticker = x.Key;
				var cacheEntry = x.Value;
				var mode = EarningsFeatureMode.Common;
				GenerateDataPointsFromEarnings(ticker, cacheEntry, null, _options.TestDate, _commonFeaturesTrainingData, mode);
				GenerateDataPointsFromEarnings(ticker, cacheEntry, _options.TestDate, null, _commonFeaturesTestData, mode);
				GenerateDataPointsFromPriceData(ticker, cacheEntry, null, _options.TestDate, _priceDataTrainingData);
				GenerateDataPointsFromPriceData(ticker, cacheEntry, _options.TestDate, null, _priceDataTestData);
			});
			var sortData = (List<DataPoint> data) => data.Sort((x, y) => x.Date.CompareTo(y.Date));
			sortData(_commonFeaturesTrainingData);
			sortData(_commonFeaturesTestData);
			sortData(_priceDataTrainingData);
			sortData(_priceDataTestData);
			stopwatch.Stop();
			Console.WriteLine($"Removed {_datasetLoader.GoodTickers} tickers ({(decimal)_datasetLoader.BadTickers / (_datasetLoader.BadTickers + _datasetLoader.GoodTickers):P1}) due to insufficient price data");
			decimal trainingOutperform = GetLabelratio(PerformanceLabelType.Outperform, _commonFeaturesTrainingData);
			decimal trainingUnderperform = GetLabelratio(PerformanceLabelType.Underperform, _commonFeaturesTrainingData);
			decimal testOutperform = GetLabelratio(PerformanceLabelType.Outperform, _commonFeaturesTestData);
			decimal testUnderperform = GetLabelratio(PerformanceLabelType.Underperform, _commonFeaturesTestData);
			Console.WriteLine($"Generated {_commonFeaturesTrainingData.Count} data points of training data ({trainingOutperform:P2} outperform, {trainingUnderperform:P2} underperform) and {_commonFeaturesTestData.Count} data points of test data ({testOutperform:P2} outperform, {testUnderperform:P2} underperform) in {stopwatch.Elapsed.TotalSeconds:F1} s");
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
				var earningsFeatures = currentEarnings.Value;
				DateTime currentDate = currentEarnings.Key + TimeSpan.FromDays(_options.DaysSinceEarnings);
				while (currentDate.DayOfWeek != DayOfWeek.Monday)
					currentDate += TimeSpan.FromDays(1);
				DateTime futureDate = GetNextWorkingDay(currentDate + TimeSpan.FromDays(_options.ForecastDays));
				decimal? performance = GetPerformanceRelativeToMarket(priceData, currentDate, futureDate);
				if (!performance.HasValue)
					continue;
				float[] features = null;
				if (mode == EarningsFeatureMode.Common)
					features = earningsFeatures.Take(_options.CommonFeatures).ToArray();
				else if (mode == EarningsFeatureMode.NominalCorrelation)
					features = GetIndexBasedFeatures(earningsFeatures, false, _nominalCorrelationFeatures);
				else if (mode == EarningsFeatureMode.Presence)
					features = GetIndexBasedFeatures(earningsFeatures, true, _presenceFeatures);
				var label = GetLabel(performance.Value);
				var dataPoint = new DataPoint
				{
					Features = features,
					Label = (UInt32)label,
					// Metadata for backtesting, not used by training
					Ticker = ticker,
					Date = currentDate,
				};
				lock (dataPoints)
					dataPoints.Add(dataPoint);
			}
		}

		private PerformanceLabelType GetLabel(decimal performance)
		{
			PerformanceLabelType label = PerformanceLabelType.Neutral;
			if (performance > _options.OutperformLimit)
				label = PerformanceLabelType.Outperform;
			else if (performance < _options.UnderperformLimit)
				label = PerformanceLabelType.Underperform;
			return label;
		}

		private void GenerateDataPointsFromPriceData(string ticker, TickerCacheEntry tickerCacheEntry, DateTime? from, DateTime? to, List<DataPoint> dataPoints)
		{
			var priceData = tickerCacheEntry.PriceData;
			if (priceData == null)
				return;

			foreach (var pair in priceData)
			{
				var currentDate = pair.Key;
				if (currentDate.DayOfWeek != DayOfWeek.Monday)
					continue;
				DateTime futureDate = GetNextWorkingDay(currentDate + TimeSpan.FromDays(_options.ForecastDays));
				if (!InRange(currentDate, from, to))
					continue;
				if (pair.Value.Open < _options.MinimumPrice)
					continue;
				var pastPrices = priceData.Values.Where(x => x.Date < currentDate).ToArray();
				if (pastPrices.Length < PriceDataMinimum)
					continue;
				var pastCloses = pastPrices.Select(x => (float)x.Close).ToArray();
				var technicalIndicatorFeatures = TechnicalIndicators.GetFeatures(pastCloses);
				var generalFeatures = new float[]
				{
					(int)currentDate.DayOfWeek
				};
				var ohlcvFeatures = new List<float>();
				foreach (var price in pastPrices.Take(5))
				{
					ohlcvFeatures.AddRange(new float[]
					{
						(float)price.Open,
						(float)price.High,
						(float)price.Low,
						(float)price.Close,
						price.Volume
					});
				}
				decimal? performance = GetPerformanceRelativeToMarket(priceData, currentDate, futureDate);
				if (!performance.HasValue)
					continue;
				var label = GetLabel(performance.Value);
				var dataPoint = new DataPoint
				{
					Features = generalFeatures.Concat(ohlcvFeatures).Concat(technicalIndicatorFeatures).ToArray(),
					Label = (UInt32)label,
					// Metadata for backtesting, not used by training
					Ticker = ticker,
					Date = currentDate,
				};
				lock (dataPoints)
					dataPoints.Add(dataPoint);
			}
		}

		private decimal? GetPerformanceRelativeToMarket(SortedList<DateTime, PriceData> priceData, DateTime currentDate, DateTime futureDate)
		{
			decimal? currentPrice = GetOpenPrice(currentDate, priceData);
			if (!currentPrice.HasValue)
				return null;
			decimal? futurePrice = GetOpenPrice(futureDate, priceData);
			if (!futurePrice.HasValue)
				return null;
			decimal? currentIndexPrice = GetOpenPrice(currentDate, _indexPriceData);
			if (!currentIndexPrice.HasValue)
				return null;
			decimal? futureIndexPrice = GetOpenPrice(futureDate, _indexPriceData);
			if (!futureIndexPrice.HasValue)
				return null;
			if (currentPrice.Value < _options.MinimumPrice)
				return null;

			decimal performance = futurePrice.Value / currentPrice.Value - futureIndexPrice.Value / currentIndexPrice.Value;
			return performance;
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

		private Predictions TrainAndEvaluateModel(List<DataPoint> trainingData, List<DataPoint> testData, IAlgorithm algorithm)
		{
			var mlContext = new MLContext();
			var schema = SchemaDefinition.Create(typeof(DataPoint));
			int featureCount = trainingData.First().Features.Length;
			schema[nameof(DataPoint.Features)].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			var trainingDataView = mlContext.Data.LoadFromEnumerable(trainingData, schema);
			var testDataView = mlContext.Data.LoadFromEnumerable(testData, schema);
			var estimator = algorithm.GetEstimator(mlContext);
			Console.WriteLine($"Training model with algorithm \"{algorithm.Name}\" using {trainingData.Count} data points with {featureCount} features each");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var model = estimator.Fit(trainingDataView);
			stopwatch.Stop();
			Console.WriteLine($"Done training model in {stopwatch.Elapsed.TotalSeconds:F1} s, performing test with {testData.Count} data points ({((decimal)testData.Count / (trainingData.Count + testData.Count)):P2} of total)");
			var trainingPredictions = model.Transform(trainingDataView);
			var testPredictions = model.Transform(testDataView);
			var predictions = new Predictions(trainingPredictions, testPredictions);
			if (PrintEvaluation)
			{
				var metrics = mlContext.MulticlassClassification.Evaluate(testPredictions);
				Console.WriteLine($"  MacroAccuracy: {metrics.MacroAccuracy:P2}");
				Console.WriteLine($"  MicroAccuracy: {metrics.MicroAccuracy:P2}");
				Console.WriteLine($"  LogLoss: {metrics.LogLoss:F3}");
				Console.WriteLine($"  LogLossReduction: {metrics.LogLossReduction:F3}");
				Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
			}
			return predictions;
		}

		private List<DataPoint> CreateMetaDataPoints(List<DataPoint> source, IDataView transformed)
		{
			var scores = transformed.GetColumn<float[]>("Score").ToArray();
			int i = 0;
			var output = scores.Select(x =>
			{
				var sourceDataPoint = source[i];
				var newDataPoint = new DataPoint
				{
					Features = scores[i],
					Label = sourceDataPoint.Label,
					Ticker = sourceDataPoint.Ticker,
					Date = sourceDataPoint.Date
				};
				i++;
				return newDataPoint;
			}).ToList();
			return output;
		}

		private void MergeMetaDataPoints(List<DataPoint> source, IDataView transformed, List<DataPoint> output)
		{
			var map = new Dictionary<DataPointKey, float[]>();
			var scores = transformed.GetColumn<float[]>("Score").ToArray();
			int i = 0;
			foreach (var dataPoint in source)
			{
				var key = new DataPointKey(dataPoint);
				map[key] = scores[i];
				i++;
			}
			foreach (var dataPoint in output)
			{
				var key = new DataPointKey(dataPoint);
				float[] features;
				if (!map.TryGetValue(key, out features))
				{
					// Hack, duplicate the features
					features = dataPoint.Features;
				}
				dataPoint.Features = dataPoint.Features.Concat(features).ToArray();
			}
		}
	}
}