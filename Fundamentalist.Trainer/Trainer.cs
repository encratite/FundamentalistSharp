using Fundamentalist.Common;
using Fundamentalist.Common.Json.FinancialStatement;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using MsgPack.Serialization;
using System.Diagnostics;

namespace Fundamentalist.Trainer
{
	internal class Trainer
	{
		private TrainerOptions _options;
		private List<PriceData> _indexPriceData = null;
		private Dictionary<string, TickerCacheEntry> _tickerCache = new Dictionary<string, TickerCacheEntry>();

		public void Run(TrainerOptions options)
		{
			_options = options;
			options.Print();
			LoadIndex();
			var dataPoints = GetDataPoints();
			TrainAndEvaluateModel(dataPoints);
		}

		private void LoadIndex()
		{
			if (_indexPriceData == null)
			{
				var indexTicker = CompanyTicker.GetIndexTicker();
				_indexPriceData = DataReader.GetPriceData(indexTicker);
			}
		}

		private List<DataPoint> GetDataPoints()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			if (_options.DataPointsPath != null)
			{
				var deserializedDataPoints = DeserializeDataPoints();
				if (deserializedDataPoints != null)
				{
					stopwatch.Stop();
					WriteInfo($"Loaded {deserializedDataPoints.Count} data points from \"{_options.DataPointsPath}\" in {stopwatch.Elapsed.TotalSeconds:F1} s");
					return deserializedDataPoints;
				}
			}
			var tickers = DataReader.GetTickers();
			var dataPoints = new List<DataPoint>();
			int tickersProcessed = 0;
			int goodTickers = 0;
			Parallel.ForEach(tickers, ticker =>
			{
				tickersProcessed++;
				var cacheEntry = GetCacheEntry(ticker, tickers, tickersProcessed);
				if (cacheEntry == null)
					return;
				GenerateDataPoints(cacheEntry.FinancialStatements, cacheEntry.PriceData, dataPoints);
				goodTickers++;
				WriteInfo($"Generated data points for {ticker.Ticker} ({tickersProcessed}/{tickers.Count}), discarded {1.0m - (decimal)goodTickers / tickersProcessed:P1} of tickers");
			});
			stopwatch.Stop();
			WriteInfo($"Generated {dataPoints.Count} data points ({(decimal)dataPoints.Count / tickers.Count:F1} per ticker) in {stopwatch.Elapsed.TotalSeconds:F1} s");
			if (_options.DataPointsPath != null)
			{
				stopwatch.Restart();
				SerializeDataPoints(dataPoints);
				WriteInfo($"Serialized data points in {stopwatch.Elapsed.TotalSeconds:F1} s");
				stopwatch.Stop();
			}
			return dataPoints;
		}

		private TickerCacheEntry GetCacheEntry(CompanyTicker ticker, List<CompanyTicker> tickers, int tickersProcessed)
		{
			TickerCacheEntry cacheEntry = null;
			string key = ticker.Ticker;
			bool hasEntry;
			lock (_tickerCache)
				hasEntry = _tickerCache.TryGetValue(key, out cacheEntry);
			if (!hasEntry)
			{
				var financialStatements = DataReader.GetFinancialStatements(ticker);
				if (financialStatements == null)
				{
					WriteInfo($"Discarded {ticker.Ticker} due to lack of financial statements ({tickersProcessed}/{tickers.Count})");
					SetCacheEntry(key, null);
					return cacheEntry;
				}
				var priceData = DataReader.GetPriceData(ticker);
				if (priceData == null)
				{
					WriteInfo($"Discarded {ticker.Ticker} due to lack of price data ({tickersProcessed}/{tickers.Count})");
					SetCacheEntry(key, null);
					return cacheEntry;
				}
				cacheEntry = new TickerCacheEntry
				{
					FinancialStatements = financialStatements,
					PriceData = priceData
				};
				SetCacheEntry(key, cacheEntry);
			}
			return cacheEntry;
		}

		private void SetCacheEntry(string key, TickerCacheEntry value)
		{
			lock (_tickerCache)
				_tickerCache[key] = value;
		}

		private void SerializeDataPoints(List<DataPoint> dataPoints)
		{
			using (var fileStream = new FileStream(_options.DataPointsPath, FileMode.Create))
			{
				var serializer = MessagePackSerializer.Get<List<DataPoint>>();
				serializer.Pack(fileStream, dataPoints);
			}
		}

		private List<DataPoint> DeserializeDataPoints()
		{
			if (File.Exists(_options.DataPointsPath))
			{
				using (var fileStream = new FileStream(_options.DataPointsPath, FileMode.Open))
				{
					var serializer = MessagePackSerializer.Get<List<DataPoint>>();
					var dataPoints = serializer.Unpack(fileStream);
					return dataPoints;
				}
			}
			else
				return null;
		}

		private void TrainAndEvaluateModel(List<DataPoint> dataPoints)
		{
			int iterations = 50;
			var mlContext = new MLContext();
			const string FeatureName = "Features";
			var schema = SchemaDefinition.Create(typeof(DataPoint));
			int featureCount = dataPoints.First().Features.Length;
			schema[FeatureName].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			var dataView = mlContext.Data.LoadFromEnumerable(dataPoints, schema);
			var splitData = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
			var options = new SgdCalibratedTrainer.Options
			{
				NumberOfIterations = iterations
			};
			var estimator =
				mlContext.Transforms.NormalizeMinMax(FeatureName)
				.AppendCacheCheckpoint(mlContext)
				.Append(mlContext.BinaryClassification.Trainers.SgdCalibrated(options));
			Console.WriteLine($"Training model with {dataPoints.Count} data points with {featureCount} features each, using {iterations} iterations");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var model = estimator.Fit(splitData.TrainSet);
			stopwatch.Stop();
			Console.WriteLine($"Done training model in {stopwatch.Elapsed.TotalSeconds:F1} s");
			var predictions = model.Transform(splitData.TestSet);
			var metrics = mlContext.BinaryClassification.Evaluate(predictions);
			Console.WriteLine($"  Accuracy: {metrics.Accuracy:P1}");
			// Console.WriteLine($"  AreaUnderRocCurve: {metrics.AreaUnderRocCurve:P1}");
			Console.WriteLine($"  F1Score: {metrics.F1Score:P1}");
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

		private void GenerateDataPoints(List<FinancialStatement> financialStatements, List<PriceData> priceData, List<DataPoint> dataPoints)
		{
			for (int i = 0; i < financialStatements.Count - _options.FinancialStatementCount; i++)
			{
				var currentFinancialStatements = financialStatements.Skip(i).Take(_options.FinancialStatementCount).ToList();
				DateTime date1 = currentFinancialStatements.Last().SourceDate.Value;
				DateTime date2 = date1.AddDays(_options.LookaheadDays);
				decimal? indexPrice1 = GetPrice(date1, _indexPriceData);
				decimal? indexPrice2 = GetPrice(date2, _indexPriceData);
				decimal? stockPrice1 = GetPrice(date1, priceData);
				decimal? stockPrice2 = GetPrice(date2, priceData);
				if (
					!indexPrice1.HasValue ||
					!indexPrice2.HasValue ||
					!stockPrice1.HasValue ||
					!stockPrice2.HasValue
				)
					continue;
				bool enableHistory = _options.HistoryDays > 0;
				List<PriceData> history = null;
				if (enableHistory)
				{
					history = priceData.Where(p => p.Date <= date1).Reverse().ToList();
					if (history.Count < _options.HistoryDays)
						continue;
					history.RemoveRange(_options.HistoryDays, history.Count - _options.HistoryDays);
				}
				decimal indexPerformance = indexPrice2.Value / indexPrice1.Value;
				decimal stockPerformance = stockPrice2.Value / stockPrice1.Value;
				decimal performance = stockPerformance - indexPerformance;
				var features = Features.GetFeatures(currentFinancialStatements).AsEnumerable();
				if (enableHistory)
				{
					var historyFeatures = new List<float>();
					foreach (var p in history)
						p.AddFeatures(historyFeatures);
					features = features.Concat(historyFeatures);
				}
				bool label = performance >= _options.MinPerformance;
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
			// Console.WriteLine(message);
		}
	}
}
