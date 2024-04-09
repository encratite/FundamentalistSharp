using Fundamentalist.Common;
using Fundamentalist.Common.Json.FinancialStatement;
using Fundamentalist.Common.Json.KeyRatios;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Fundamentalist.Trainer
{
	internal class Trainer
	{
		private TrainerOptions _options;
		private List<PriceData> _indexPriceData = null;
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
			TrainAndEvaluateModel();
			_trainingData = null;
			_testData = null;
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
				GenerateDataPoints(ticker.Ticker, cacheEntry, null, _options.SplitDate, _trainingData);
				GenerateDataPoints(ticker.Ticker, cacheEntry, _options.SplitDate, null, _testData);
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
			List<PriceData> priceData = null;
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

		private decimal? GetPrice(DateTime date, List<PriceData> priceData)
		{
			PriceData previousPrice = null;
			foreach (var price in priceData)
			{
				if (price.Date == date)
					return price.Mean;
				else if (previousPrice != null && previousPrice.Date >= date && price.Date > date)
					return previousPrice.Mean;
				previousPrice = price;
			}
			return null;
		}

		private bool InRange(DateTime? date, DateTime? from, DateTime? to)
		{
			return
				(!from.HasValue || date.Value >= from.Value) &&
				(!to.HasValue || date.Value < to.Value);
		}

		private float GetRelativeChange(float previous, float current)
		{
			const float Maximum = 100.0f;
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

		private List<float> GetFinancialFeatures(FinancialStatement previousStatement, FinancialStatement currentStatement)
		{
			var previousFeatures = Features.GetFeatures(previousStatement);
			var currentFeatures = Features.GetFeatures(currentStatement);
			var features = new List<float>();
			for (int i = 0; i < previousFeatures.Count; i++)
			{
				float previous = previousFeatures[i];
				float current = currentFeatures[i];
				float relativeChange = GetRelativeChange(previous, current);
				features.Add(relativeChange);
			}
			return features;
		}

		private void GenerateDataPoints(string ticker, TickerCacheEntry tickerCacheEntry, DateTime? from, DateTime? to, List<DataPoint> dataPoints)
		{
			var financialStatements = tickerCacheEntry.FinancialStatements.Where(x => InRange(x.SourceDate, from, to)).ToList();
			var priceData = tickerCacheEntry.PriceData;
			var reverseMetrics = tickerCacheEntry.KeyRatios.CompanyMetrics.AsEnumerable().Reverse();

			for (int i = 0; i < financialStatements.Count - 1; i++)
			{
				var successiveStatements = financialStatements.Skip(i).Take(2).ToList();
				var previousStatement = successiveStatements[0];
				var currentStatement = successiveStatements[1];

				DateTime currentDate = currentStatement.SourceDate.Value;
				DateTime pastDate = currentDate - TimeSpan.FromDays(_options.HistoryDays);
				DateTime futureDate = currentDate + TimeSpan.FromDays(_options.ForecastDays);

				var keyRatios = reverseMetrics.FirstOrDefault(x => x.EndDate.Value <= currentDate);
				if (keyRatios == null)
				{
					Interlocked.Increment(ref _keyRatioErrors);
					continue;
				}

				decimal? currentPrice = GetPrice(currentDate, priceData);
				decimal? pastPrice = GetPrice(pastDate, priceData);
				decimal? futurePrice = GetPrice(futureDate, priceData);
				if (
					!currentPrice.HasValue ||
					!pastPrice.HasValue ||
					!futurePrice.HasValue
				)
				{
					Interlocked.Increment(ref _priceErrors);
					continue;
				}

				var financialFeatures = GetFinancialFeatures(previousStatement, currentStatement);
				var keyRatioFeatures = Features.GetFeatures(keyRatios);
				float pastPerformance = GetRelativeChange(pastPrice, currentPrice);
				float futurePerformance = GetRelativeChange(currentPrice, futurePrice);
				var performanceFeatures = new float[] { pastPerformance };
				var features = financialFeatures.Concat(keyRatioFeatures).Concat(performanceFeatures);
				var dataPoint = new DataPoint
				{
					Ticker = ticker,
					Features = features.ToArray(),
					Label = futurePerformance,
					Date = currentDate,
					PriceData = priceData
				};
				lock (dataPoints)
					dataPoints.Add(dataPoint);
			}
		}

		private void TrainAndEvaluateModel()
		{
			int iterations = 100;
			var mlContext = new MLContext();
			const string FeatureName = "Features";
			var schema = SchemaDefinition.Create(typeof(DataPoint));
			int featureCount = _trainingData.First().Features.Length;
			schema[FeatureName].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			var trainingData = mlContext.Data.LoadFromEnumerable(_trainingData, schema);
			var testData = mlContext.Data.LoadFromEnumerable(_testData, schema);
			var estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.Append(mlContext.Regression.Trainers.Sdca(maximumNumberOfIterations: iterations));
			Console.WriteLine($"Training model with {_trainingData.Count} data points with {featureCount} features each, using {iterations} iterations");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var model = estimator.Fit(trainingData);
			stopwatch.Stop();
			Console.WriteLine($"Done training model in {stopwatch.Elapsed.TotalSeconds:F1} s, performing test with {_testData.Count} data points ({((decimal)_testData.Count / (_trainingData.Count + _testData.Count)):P2} of total)");
			var predictions = model.Transform(testData);
			SetScores(predictions);
			var metrics = mlContext.Regression.Evaluate(predictions);
			Console.WriteLine($"  LossFunction: {metrics.LossFunction:F3}");
			Console.WriteLine($"  MeanAbsoluteError: {metrics.MeanAbsoluteError:F3}");
			Console.WriteLine($"  MeanSquaredError: {metrics.MeanSquaredError:F3}");
			Console.WriteLine($"  RootMeanSquaredError: {metrics.RootMeanSquaredError:F3}");
			Console.WriteLine($"  RSquared: {metrics.RSquared:F3}");
			_options.Print();
			Backtest();
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

		private void Backtest()
		{
			const decimal InitialMoney = 100000.0m;
			const decimal MinimumInvestment = 10000.0m;
			const decimal Fee = 10.0m;
			const int PortfolioStocks = 5;
			const int RebalanceDays = 30;
			const int HistoryDays = 15;

			decimal money = InitialMoney;
			var portfolio = new List<Stock>();

			Console.WriteLine("Performing backtest");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			DateTime now = _testData.First().Date;

			var log = (string message) => Console.WriteLine($"[{now.ToShortDateString()}] {message}");

			var sellStocks = () =>
			{
				foreach (var stock in portfolio)
				{
					var priceData = stock.Data.PriceData;
					decimal? currentPrice = GetPrice(now, priceData);
					if (currentPrice == null)
						currentPrice = priceData.Last().Mean;
					decimal ratio = currentPrice.Value / stock.BuyPrice;
					decimal change = ratio - 1.0m;
					decimal sellPrice = ratio * stock.InitialInvestment;
					decimal gain = change * stock.InitialInvestment;
					money += sellPrice - Fee;
					if (ratio > 1.0m)
						log($"Gained {gain:C0} ({change:+#.00%;-#.00%;+0.00%}) from selling {stock.Data.Ticker} (prediction {stock.Data.Score:F3})");
					else
						log($"Lost {Math.Abs(gain):C0} ({change:+#.00%;-#.00%;+0.00%}) on {stock.Data.Ticker} (prediction {stock.Data.Score:F3})");
				}
				portfolio.Clear();
			};

			DateTime finalDate = _testData.Last().Date + TimeSpan.FromDays(_options.ForecastDays);
			for (; now < finalDate; now += TimeSpan.FromDays(RebalanceDays))
			{
				// Sell all previously owned stocks
				sellStocks();

				if (money < MinimumInvestment)
				{
					log("Ran out of money");
					break;
				}

				log($"Rebalancing portfolio with {money:C0} in the bank");

				var available =
					_testData.Where(x => x.Date >= now - TimeSpan.FromDays(HistoryDays) && x.Date <= now)
					.OrderByDescending(x => x.Score)
					.ToList();
				if (!available.Any())
				{
					log("No recent financial statements available");
					continue;
				}

				// Rebalance portfolio by buying new stocks
				int count = Math.Min(PortfolioStocks, available.Count);
				decimal investment = (money - 2 * count * Fee) / count;
				foreach (var data in available)
				{
					if (portfolio.Any(x => x.Data.Ticker == data.Ticker))
					{
						// Make sure we don't accidentally buy the same stock twice
						continue;
					}
					decimal currentPrice = GetPrice(now, data.PriceData).Value;
					var stock = new Stock
					{
						InitialInvestment = investment,
						BuyDate = now,
						BuyPrice = currentPrice,
						Data = data
					};
					money -= investment + Fee;
					portfolio.Add(stock);
					if (portfolio.Count >= PortfolioStocks)
						break;
				}
			}
			// Cash out
			sellStocks();

			stopwatch.Stop();
			Console.WriteLine($"Finished backtest from {_options.SplitDate.ToShortDateString()} to {now.ToShortDateString()} with {money:C0} in the bank ({money / InitialMoney - 1.0m:+#.00%;-#.00%;+0.00%})");
		}
	}
}
