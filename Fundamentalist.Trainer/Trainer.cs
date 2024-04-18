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
				new Sdca(100, null, null),
				new OnlineGradientDescent(100, 0.01f, 0.0f),
				new LightGbmRegression(100, null, null),
				new FastTree(20, 100),
				new FastTreeTweedie(20, 100, 10),
				new FastForest(20, 100, 10),
				new Gam(100, 255),
			};
			Backtest backtest = null;
			foreach (var algorithm in algorithms)
			{
				string scoreName = $"{algorithm.Name}-{_options.ForecastDays}";
				TrainAndEvaluateModel(algorithm);
				// DumpScores(scoreName);
				backtest = new Backtest(_testData, _indexPriceData, minimumGain: _options.MinimumGain);
				decimal performance = backtest.Run();
				logPerformance(algorithm.Name, performance);
			}

			if (backtest != null)
			{
				logPerformance("S&P 500", backtest.IndexPerformance);
				Console.WriteLine("Options used:");
				_options.Print();
				Console.WriteLine("Performance summary:");
				int maxPadding = backtestLog.MaxBy(x => x.Description.Length).Description.Length + 1;
				foreach (var entry in backtestLog)
					Console.WriteLine($"  {(entry.Description + ":").PadRight(maxPadding)} {entry.Performance:#.00%}");
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
				_datasetLoader.Load(_earningsPath, _priceDataDirectory, _options.Features, PriceDataMinimum);
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
			Console.WriteLine($"Generated {_trainingData.Count} data points of training data and {_testData.Count} data points of test data in {stopwatch.Elapsed.TotalSeconds:F1} s");
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
				float futurePrice = (float)futurePrices[_options.ForecastDays - 1].Close;

				var signFeatures = earningsFeatures.Take(_options.Features).Select(x => (float)Math.Sign(x));

				var priceDataFeatures = TechnicalIndicators.GetFeatures(currentPrice, pastPrices);
				float label = futurePrice;
				var features = signFeatures.Concat(priceDataFeatures);
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

		private float GetChange(float previous, float current)
		{
			if (previous == 0 && current == 0)
				return 0;
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
				var metrics = mlContext.Regression.Evaluate(predictions);
				// Console.WriteLine($"  LossFunction: {metrics.LossFunction:F3}");
				Console.WriteLine($"  MeanAbsoluteError: {metrics.MeanAbsoluteError:F3}");
				Console.WriteLine($"  MeanSquaredError: {metrics.MeanSquaredError:F3}");
				Console.WriteLine($"  RootMeanSquaredError: {metrics.RootMeanSquaredError:F3}");
				Console.WriteLine($"  RSquared: {metrics.RSquared:F3}");
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

		private void DumpScores(string name, string directory)
		{
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, $"{name}.csv");
			using (var writer = new StringWriter())
			{
				writer.WriteLine("Score,Performance");
				foreach (var dataPoint in _testData)
				{
					writer.WriteLine($"{dataPoint.Score},{dataPoint.Label}");
				}
				File.WriteAllText(path, writer.ToString());
			}
		}
	}
}
