using Fundamentalist.Common;
using Fundamentalist.Trainer.Algorithm;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fundamentalist.Trainer
{
	internal class Trainer
	{
		private const int PriceDataMinimum = 200;

		private TrainerOptions _options;
		private string _earningsPath;
		private string _priceDataDirectory;

		private SortedList<DateTime, PriceData> _indexPriceData = null;
		private Dictionary<string, TickerCacheEntry> _tickerCache = new Dictionary<string, TickerCacheEntry>();

		private List<DataPoint> _trainingData;
		private List<DataPoint> _testData;

		private int _badTickers = 0;
		private int _goodTickers = 0;

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
				new Sdca(),

				new OnlineGradientDescent(),

				// new LightGbmRegression(20, 1),
				// new LightGbmRegression(20, 100),
				// new LightGbmRegression(50, 100),
				// new LightGbmRegression(50, 200),
				// new LightGbmRegression(200, 100),
				// new LightGbmRegression(200, 200),
				// new LightGbmRegression(200, 500),
				// new LightGbmRegression(null, 100),
				// Best:
				new LightGbmRegression(null, 100, null, 1000),
				// new LightGbmRegression(null, 100, 1e-4, 1000),
				// new LightGbmRegression(null, 100, 1e-5, 1000),

				// PCA breaks it?
				new FastTree(null, 20, 100),
				// new FastTree(20, 20, 100),
				// new FastTree(50, 15, 80),
				// Best, but most of them are similar:
				// new FastTree(50, 10, 50),
				// new FastTree(50, 5, 20),
				// new FastTree(100, 20, 100),
				// new FastTree(100, 50, 100),
				// new FastTree(100, 20, 500),
				// new FastTree(100, 50, 1000),
				// new FastTree(250, 20, 100),
				// new FastTree(null, 50, 100),
				// new FastTree(null, 20, 1000),

				// Best out of all:
				new FastTreeTweedie(null, 20, 100, 10),
				// new FastTreeTweedie(null, 20, 75, 10),
				// new FastTreeTweedie(null, 15, 100, 10),
				// new FastTreeTweedie(null, 20, 100, 15),
				// new FastTreeTweedie(null, 20, 100, 5),
				// new FastTreeTweedie(null, 20, 100, 50),
				// More leaves/trees make it worse:
				// new FastTreeTweedie(null, 50, 100, 10),
				// new FastTreeTweedie(null, 20, 1000, 10),
				// PCA seems to make it worse:
				// new FastTreeTweedie(20, 20, 100, 10),
				// new FastTreeTweedie(50, 15, 80, 10),
				// new FastTreeTweedie(50, 10, 50, 10),
				// new FastTreeTweedie(50, 5, 20, 10),
				// new FastTreeTweedie(100, 20, 100, 10),
				// new FastTreeTweedie(100, 50, 100, 10),
				// new FastTreeTweedie(100, 20, 500, 10),
				// new FastTreeTweedie(100, 50, 1000, 10),
				// new FastTreeTweedie(250, 20, 100, 10),

				// new FastForest(null, 20, 100, 10),
				// new FastForest(null, 20, 75, 10),
				// new FastForest(null, 15, 100, 10),
				// new FastForest(null, 20, 100, 15),
				// new FastForest(null, 20, 100, 5),
				// new FastForest(null, 20, 100, 50),
				// new FastForest(null, 50, 100, 10),
				// Best:
				new FastForest(null, 20, 1000, 10),
				// PCA seems to completely break, returns all zeroes
				// new FastForest(20, 20, 100, 10),
				// new FastForest(50, 15, 80, 10),
				// new FastForest(50, 10, 50, 10),
				// new FastForest(50, 5, 20, 10),
				// new FastForest(100, 20, 100, 10),
				// new FastForest(100, 50, 100, 10),
				// new FastForest(100, 20, 500, 10),
				// new FastForest(100, 50, 1000, 10),
				// new FastForest(250, 20, 100, 10),

				// More PCA breakage
				new Gam(null, 100, 255),
				// new Gam(null, 9500, 255),
				// new Gam(null, 5000, 255),
				// new Gam(null, 100, 255),
				// new Gam(null, 50, 255),
				// new Gam(null, 25, 255),
				// new Gam(null, 150, 255),
				// new Gam(null, 100, 300),
				// new Gam(null, 100, 500),
				// new Gam(null, 100, 100),
				// These took too long, didn't even finish:
				// new Gam(null, 15000, 255),
				// new Gam(null, 9500, 150),
				// new Gam(null, 9500, 50),
				// new Gam(null, 9500, 500),
				// new Gam(20, 100, 255),
				// Best:
				// new Gam(100, 100, 255),
				// new Gam(100, 150, 255),
				// new Gam(100, 200, 255),
				// new Gam(100, 100, 1000),
				// new Gam(100, 150, 1000),
				// new Gam(100, 200, 1000),
				// new Gam(250, 100, 255),
			};
			Backtest backtest = null;
			foreach (var algorithm in algorithms)
			{
				string scoreName = $"{algorithm.Name}-{_options.ForecastDays}";
				TrainAndEvaluateModel(algorithm);
				// DumpScores(scoreName);
				backtest = new Backtest(_testData, _indexPriceData);
				decimal performance = backtest.Run();
				logPerformance(algorithm.Name, performance);
			}

			logPerformance("S&P 500", backtest.IndexPerformance);
			Console.WriteLine("Options used:");
			_options.Print();
			Console.WriteLine("Performance summary:");
			int maxPadding = backtestLog.MaxBy(x => x.Description.Length).Description.Length + 1;
			foreach (var entry in backtestLog)
				Console.WriteLine($"  {(entry.Description + ":").PadRight(maxPadding)} {entry.Performance:#.00%}");
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
			if (_tickerCache.Count == 0)
			{
				Console.WriteLine("Loading earnings");
				LoadEarnings();
				stopwatch.Stop();
				Console.WriteLine($"Loaded earnings in {stopwatch.Elapsed.TotalSeconds:F1} s");
				Console.WriteLine("Loading price data");
				stopwatch.Restart();
				LoadPriceData();
				stopwatch.Stop();
				Console.WriteLine($"Loaded price data in {stopwatch.Elapsed.TotalSeconds:F1} s");
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
			Console.WriteLine($"Removed {_badTickers} tickers ({(decimal)_badTickers / (_badTickers + _goodTickers):P1}) due to insufficient price data");
			Console.WriteLine($"Generated {_trainingData.Count} data points of training data and {_testData.Count} data points of test data in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void LoadEarnings()
		{
			var earningLines = DataReader.GetEarnings(_earningsPath);
			foreach (var x in earningLines)
			{
				string ticker = x.Ticker;
				TickerCacheEntry cacheEntry;
				if (!_tickerCache.TryGetValue(ticker, out cacheEntry))
				{
					cacheEntry = new TickerCacheEntry();
					_tickerCache[ticker] = cacheEntry;
				}
				cacheEntry.Earnings.Add(x.Date, x.Features);
			}
		}

		private void LoadPriceData()
		{
			var options = new ParallelOptions
			{
				MaxDegreeOfParallelism = 4
			};
			Parallel.ForEach(_tickerCache.ToList(), options, x =>
			{
				string ticker = x.Key;
				var cacheEntry = x.Value;
				var priceData = DataReader.GetPriceData(ticker, _priceDataDirectory);
				if (priceData == null || priceData.Count < PriceDataMinimum)
				{
					_tickerCache.Remove(ticker);
					Interlocked.Increment(ref _badTickers);
					return;
				}
				cacheEntry.PriceData = priceData;
				Interlocked.Increment(ref _goodTickers);
			});
		}

		private bool InRange(DateTime? date, DateTime? from, DateTime? to)
		{
			return
				(!from.HasValue || date.Value >= from.Value) &&
				(!to.HasValue || date.Value < to.Value);
		}

		private float GetSimpleMovingAverage(int days, float[] prices)
		{
			float simpleMovingAverage = prices.TakeLast(days).Sum() / days;
			return simpleMovingAverage;
		}

		private float GetExponentialMovingAverage(int days, float[] prices)
		{
			var exponentialMovingAverage = new float[prices.Length];
			exponentialMovingAverage[days - 1] = GetSimpleMovingAverage(days, prices);
			float weight = 2.0f / (days + 1);
			for (int i = days; i < prices.Length; i++)
				exponentialMovingAverage[i] = weight * prices[i] + (1 - weight) * exponentialMovingAverage[i - 1];
			return exponentialMovingAverage[prices.Length - 1];
		}

		private float GetRelativeStrengthIndex(int days, float[] prices)
		{
			float gains = 0.0f;
			float losses = 0.0f;
			for (int i = prices.Length - days; i < prices.Length; i++)
			{
				float change = prices[i] / prices[i - 1] - 1.0f;
				if (change >= 0)
					gains += change;
				else
					losses -= change;
			}
			float relativeStrengthIndex = 100.0f - 100.0f / (1.0f + gains / losses);
			return relativeStrengthIndex;
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

				var priceDataFeatures = GetPriceDataFeatures(currentPrice, pastPrices);
				float label = futurePrice;
				var features = earningsFeatures.Concat(priceDataFeatures);
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

		private List<float> GetPriceDataFeatures(decimal? currentPrice, float[] pastPrices)
		{
			float sma10 = GetSimpleMovingAverage(10, pastPrices);
			float sma20 = GetSimpleMovingAverage(20, pastPrices);
			float sma50 = GetSimpleMovingAverage(50, pastPrices);
			float sma200 = GetSimpleMovingAverage(200, pastPrices);
			float ema12 = GetExponentialMovingAverage(12, pastPrices);
			float ema26 = GetExponentialMovingAverage(26, pastPrices);
			float ema50 = GetExponentialMovingAverage(50, pastPrices);
			float ema200 = GetExponentialMovingAverage(200, pastPrices);
			float macd = ema12 - ema26;
			float rsi = GetRelativeStrengthIndex(14, pastPrices);
			var priceDataFeatures = new List<float>
				{
					(float)currentPrice.Value,
					sma10,
					sma20,
					sma50,
					sma200,
					ema12,
					ema26,
					ema50,
					ema200,
					macd,
					rsi
				};
			return priceDataFeatures;
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
			/*
			var metrics = mlContext.Regression.Evaluate(predictions);
			Console.WriteLine($"  LossFunction: {metrics.LossFunction:F3}");
			Console.WriteLine($"  MeanAbsoluteError: {metrics.MeanAbsoluteError:F3}");
			Console.WriteLine($"  MeanSquaredError: {metrics.MeanSquaredError:F3}");
			Console.WriteLine($"  RootMeanSquaredError: {metrics.RootMeanSquaredError:F3}");
			Console.WriteLine($"  RSquared: {metrics.RSquared:F3}");
			*/
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
