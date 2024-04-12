using Fundamentalist.Common;
using Fundamentalist.Common.Json.FinancialStatement;
using Fundamentalist.Common.Json.KeyRatios;
using Fundamentalist.Trainer.Algorithm;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;

namespace Fundamentalist.Trainer
{
	internal class Trainer
	{
		private TrainerOptions _options;
		private SortedList<DateTime, PriceData> _indexPriceData = null;
		private Dictionary<string, TickerCacheEntry> _tickerCache = new Dictionary<string, TickerCacheEntry>();

		private List<DataPoint> _trainingData;
		private List<DataPoint> _testData;

		private int _keyRatioErrors = 0;
		private int _priceErrors = 0;


		public void Run(TrainerOptions options)
		{
			_options = options;
			_keyRatioErrors = 0;
			_priceErrors = 0;
			LoadIndex();
			GetDataPoints();

			var backtestLog = new List<PerformanceData>();
			var logPerformance = (string description, decimal performance) =>
			{
				var performanceData = new PerformanceData(description, performance);
				backtestLog.Add(performanceData);
			};

			const int Runs = 10;
			var algorithms = new IAlgorithm[]
			{
				new Sdca(),
				new OnlineGradientDescent(),
				new LightGbmRegression(),
				new FastTree(),
				new FastTreeTweedie(),
				new FastForest(),
				new Gam(),
			};
			Backtest backtest = null;
			foreach (var algorithm in algorithms)
			{
				string scoreName = $"{algorithm.Name}-{_options.ForecastDays}";
				if (algorithm.IsStochastic)
				{
					decimal performanceSum = 0.0m;
					for (int i = 0; i < Runs; i++)
					{
						bool logging = i == 0;
						TrainAndEvaluateModel(algorithm, logging);
						DumpScores(scoreName);
						backtest = new Backtest(_testData, _indexPriceData);
						decimal performance = backtest.Run();
						performanceSum += performance;
					}
					decimal meanPerformance = performanceSum / Runs;
					Console.WriteLine($"Mean performance over {Runs} runs: {meanPerformance:+#.00%;-#.00%;+0.00%}");
					logPerformance(algorithm.Name, meanPerformance);
				}
				else
				{
					TrainAndEvaluateModel(algorithm, true);
					DumpScores(scoreName);
					backtest = new Backtest(_testData, _indexPriceData);
					decimal performance = backtest.Run();
					logPerformance(algorithm.Name, performance);
				}
			}

			logPerformance("S&P 500", backtest.IndexPerformance);
			Console.WriteLine("Options used:");
			_options.Print();
			Console.WriteLine("Performance summary:");
			int maxPadding = backtestLog.MaxBy(x => x.Description.Length).Description.Length + 1;
			foreach (var entry in backtestLog)
				Console.WriteLine($"  {(entry.Description + ":").PadRight(maxPadding)} {entry.Performance:#.00%}");

			_trainingData = null;
			_testData = null;
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
				var indexTicker = CompanyTicker.GetIndexTicker();
				_indexPriceData = DataReader.GetPriceData(indexTicker);
			}
		}

		private void GetDataPoints()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var tickers = DataReader.GetTickers();
			_trainingData = new List<DataPoint>();
			_testData = new List<DataPoint>();
			int tickersProcessed = 0;
			int goodTickers = 0;
			Console.WriteLine("Generating data points");
			Parallel.ForEach(tickers, ticker =>
			{
				tickersProcessed++;
				var cacheEntry = GetCacheEntry(ticker, tickers, tickersProcessed);
				if (cacheEntry == null)
					return;
				GenerateDataPoints(ticker.Ticker, cacheEntry, null, _options.TestDate, _trainingData);
				GenerateDataPoints(ticker.Ticker, cacheEntry, _options.TestDate, null, _testData);
				goodTickers++;
				// Console.WriteLine($"Generated data points for {ticker.Ticker} ({tickersProcessed}/{tickers.Count}), discarded {1.0m - (decimal)goodTickers / tickersProcessed:P1} of tickers");
			});
			_testData.Sort((x, y) => x.Date.CompareTo(y.Date));
			stopwatch.Stop();
			decimal percentage = 1.0m - (decimal)goodTickers / tickersProcessed;
			Console.WriteLine($"Discarded {percentage:P1} of tickers due to missing data, also encountered {_keyRatioErrors} key ratio errors and {_priceErrors} price errors");
			Console.WriteLine($"Generated {_trainingData.Count} data points of training data and {_testData.Count} data points of test data in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private TickerCacheEntry GetCacheEntry(CompanyTicker ticker, List<CompanyTicker> tickers, int tickersProcessed)
		{
			const bool EnableFinancialStatements = true;
			const bool EnableKeyRatios = true;
			const bool EnablePriceData = true;

			TickerCacheEntry cacheEntry = null;
			string key = ticker.Ticker;
			bool hasEntry;
			lock (_tickerCache)
				hasEntry = _tickerCache.TryGetValue(key, out cacheEntry);
			if (hasEntry)
				return cacheEntry;
			var error = (string reason) =>
			{
				// Console.WriteLine($"Discarded {ticker.Ticker} due to lack of {reason} ({tickersProcessed}/{tickers.Count})");
				SetCacheEntry(key, null);
			};
			List<FinancialStatement> financialStatements = null;
			if (EnableFinancialStatements)
			{
				financialStatements = DataReader.GetFinancialStatements(ticker);
				if (financialStatements == null)
				{
					error("financial statements");
					return null;
				}
			}
			KeyRatios keyRatios = null;
			if (EnableKeyRatios)
			{
				keyRatios = DataReader.GetKeyRatios(ticker);
				if (keyRatios == null)
				{
					error("key ratios");
					return null;
				}
			}
			SortedList<DateTime, PriceData> priceData = null;
			if (EnablePriceData)
			{
				priceData = DataReader.GetPriceData(ticker);
				if (priceData == null)
				{
					error("price data");
					return null;
				}
			}
			cacheEntry = new TickerCacheEntry
			{
				FinancialStatements = financialStatements,
				KeyRatios = keyRatios,
				PriceData = priceData
			};
			SetCacheEntry(key, cacheEntry);
			return cacheEntry;
		}

		private void SetCacheEntry(string key, TickerCacheEntry value)
		{
			lock (_tickerCache)
				_tickerCache[key] = value;
		}

		private bool InRange(DateTime? date, DateTime? from, DateTime? to)
		{
			return
				(!from.HasValue || date.Value >= from.Value) &&
				(!to.HasValue || date.Value < to.Value);
		}

		private float GetRelativeChange(float previous, float current)
		{
			const float Maximum = 5.0f;
			const float Minimum = -Maximum;
			const float Epsilon = 1e-3f;
			if (previous == 0.0f)
				previous = Epsilon;
			float relativeChange = current / previous - 1.0f;
			if (relativeChange < Minimum)
				return Minimum;
			else if (relativeChange > Maximum)
				return Maximum;
			else
				return relativeChange;
		}

		private float GetRelativeChange(decimal? previous, decimal? current)
		{
			return GetRelativeChange((float)previous.Value, (float)current.Value);
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
			var financialStatements = tickerCacheEntry.FinancialStatements.Where(x => InRange(x.SourceDate, from, to) && x.SourceDate.Value >= _options.TrainingDate).ToList();
			var priceData = tickerCacheEntry.PriceData;
			var reverseMetrics = tickerCacheEntry.KeyRatios.CompanyMetrics.AsEnumerable().Reverse();

			foreach (var financialStatement in financialStatements)
			{
				DateTime currentDate = financialStatement.SourceDate.Value;
				DateTime futureDate = currentDate + TimeSpan.FromDays(_options.ForecastDays);

				var keyRatios = reverseMetrics.FirstOrDefault(x => x.EndDate.Value <= currentDate);
				if (keyRatios == null)
				{
					Interlocked.Increment(ref _keyRatioErrors);
					continue;
				}

				decimal? currentPrice = GetOpenPrice(currentDate, priceData);
				if (!currentPrice.HasValue)
				{
					Interlocked.Increment(ref _priceErrors);
					continue;
				}

				var pastPrices = priceData.Values.Where(x => x.Date < currentDate).Select(x => (float)x.Close).ToArray();
				if (pastPrices.Length < 200)
				{
					Interlocked.Increment(ref _priceErrors);
					continue;
				}

				var futurePrices = priceData.Values.Where(x => x.Date > currentDate).ToList();
				if (futurePrices.Count < _options.ForecastDays)
				{
					Interlocked.Increment(ref _priceErrors);
					continue;
				}
				float futurePrice = (float)futurePrices[_options.ForecastDays - 1].Close;

				var financialFeatures = Features.GetFeatures(financialStatement);
				var keyRatioFeatures = Features.GetFeatures(keyRatios);
				var priceDataFeatures = GetPriceDataFeatures(currentPrice, pastPrices);
				float label = futurePrice;
				var features = financialFeatures.Concat(keyRatioFeatures).Concat(priceDataFeatures);
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

		private void TrainAndEvaluateModel(IAlgorithm algorithm, bool logging)
		{
			var log = (string message) =>
			{
				if (logging)
					Console.WriteLine(message);
			};
			var mlContext = new MLContext();
			const string FeatureName = "Features";
			var schema = SchemaDefinition.Create(typeof(DataPoint));
			int featureCount = _trainingData.First().Features.Length;
			schema[FeatureName].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			var trainingData = mlContext.Data.LoadFromEnumerable(_trainingData, schema);
			var testData = mlContext.Data.LoadFromEnumerable(_testData, schema);
			var estimator = algorithm.GetEstimator(mlContext);
			log($"Training model with algorithm \"{algorithm.Name}\" using {_trainingData.Count} data points with {featureCount} features each");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var model = estimator.Fit(trainingData);
			stopwatch.Stop();
			log($"Done training model in {stopwatch.Elapsed.TotalSeconds:F1} s, performing test with {_testData.Count} data points ({((decimal)_testData.Count / (_trainingData.Count + _testData.Count)):P2} of total)");
			var predictions = model.Transform(testData);
			SetScores(predictions);
			/*
			var metrics = mlContext.Regression.Evaluate(predictions);
			log($"  LossFunction: {metrics.LossFunction:F3}");
			log($"  MeanAbsoluteError: {metrics.MeanAbsoluteError:F3}");
			log($"  MeanSquaredError: {metrics.MeanSquaredError:F3}");
			log($"  RootMeanSquaredError: {metrics.RootMeanSquaredError:F3}");
			log($"  RSquared: {metrics.RSquared:F3}");
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

		private void DumpScores(string name)
		{
			string directory = Path.Combine(Configuration.DataDirectory, Configuration.ScoresDirectory);
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
