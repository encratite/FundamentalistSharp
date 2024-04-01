using Fundamentalist.Common;
using Fundamentalist.Common.Json.FinancialStatement;
using Fundamentalist.Common.Json.KeyRatios;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Diagnostics;

namespace Fundamentalist.Trainer
{
	internal class Trainer
	{
		private TrainerOptions _options;
		private List<PriceData> _indexPriceData = null;
		private Dictionary<string, TickerCacheEntry> _tickerCache = new Dictionary<string, TickerCacheEntry>();

		private List<DataPoint> _trainingData;
		private List<DataPoint> _testData;

		public void Run(TrainerOptions options)
		{
			_options = options;
			LoadIndex();
			GetDataPoints();
			TrainAndEvaluateModel();
			options.Print();
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
			Parallel.ForEach(tickers, ticker =>
			{
				tickersProcessed++;
				var cacheEntry = GetCacheEntry(ticker, tickers, tickersProcessed);
				if (cacheEntry == null)
					return;
				GenerateDataPoints(cacheEntry, null, _options.SplitDate, _trainingData);
				GenerateDataPoints(cacheEntry, _options.SplitDate, null, _testData);
				goodTickers++;
				WriteInfo($"Generated data points for {ticker.Ticker} ({tickersProcessed}/{tickers.Count}), discarded {1.0m - (decimal)goodTickers / tickersProcessed:P1} of tickers");
			});
			stopwatch.Stop();
			WriteInfo($"Generated {_trainingData.Count} data points of training data and {_testData.Count} data points of test data in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private TickerCacheEntry GetCacheEntry(CompanyTicker ticker, List<CompanyTicker> tickers, int tickersProcessed)
		{
			const bool EnableFinancialStatements = false;
			const bool EnableKeyRatios = false;
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
				WriteInfo($"Discarded {ticker.Ticker} due to lack of {reason} ({tickersProcessed}/{tickers.Count})");
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
			var estimator = mlContext.Regression.Trainers.Sdca(maximumNumberOfIterations: iterations);
			Console.WriteLine($"Training model with {_trainingData.Count} data points with {featureCount} features each, using {iterations} iterations");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var model = estimator.Fit(trainingData);
			stopwatch.Stop();
			Console.WriteLine($"  Done training model in {stopwatch.Elapsed.TotalSeconds:F1} s, performing test with {_testData.Count} data points ({((decimal)_testData.Count / (_trainingData.Count + _testData.Count)):P2} of total)");
			var predictions = model.Transform(testData);
			var metrics = mlContext.Regression.Evaluate(predictions);
			Console.WriteLine($"  LossFunction: {metrics.LossFunction:F3}");
			Console.WriteLine($"  MeanAbsoluteError: {metrics.MeanAbsoluteError:F3}");
			Console.WriteLine($"  MeanSquaredError: {metrics.MeanSquaredError:F3}");
			Console.WriteLine($"  RootMeanSquaredError: {metrics.RootMeanSquaredError:F3}");
			// Console.WriteLine($"  RSquared: {metrics.RSquared:F3}");
		}

		private decimal? GetPrice(DateTime date, List<PriceData> priceData)
		{
			PriceData previousPrice = null;
			foreach (var price in priceData)
			{
				if (price.Date == date)
					return price.Open;
				else if (previousPrice != null && previousPrice.Date >= date && price.Date > date)
					return previousPrice.Open;
				previousPrice = price;
			}
			return null;
		}

		private void GenerateDataPoints(TickerCacheEntry tickerCacheEntry, DateTime? from, DateTime? to, List<DataPoint> dataPoints)
		{
			var priceData = tickerCacheEntry.PriceData.Where(p =>
				(!from.HasValue || p.Date.Value >= from.Value) &&
				(!to.HasValue || p.Date.Value < to.Value)
			).ToList();
			for (int i = 0; i < priceData.Count - _options.HistoryDays - 1; i++)
			{
				var series = priceData.Skip(i).Take(_options.HistoryDays);
				var future = priceData[i + _options.HistoryDays + 1];
				var features = new List<float>();
				var getRatio = (decimal? x, decimal? y) => (float)(x.Value / y.Value - 1.0m);
				foreach (var sample in series)
				{
					float performance = getRatio(sample.Close, sample.Open);
					features.Add(performance);
				}
				float label = getRatio(future.Close, series.Last().Close);
				var dataPoint = new DataPoint
				{
					Features = features.ToArray(),
					Label = label
				};
				lock (dataPoints)
					dataPoints.Add(dataPoint);
			}
		}

		private void WriteInfo(string message)
		{
			const bool EnableVerboseOutput = false;
			if (EnableVerboseOutput)
				Console.WriteLine(message);
		}
	}
}
