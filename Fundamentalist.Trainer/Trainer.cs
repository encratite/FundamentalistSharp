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
		private int _financialStatementCount;
		private int _lookAheadDays;
		private decimal _minOutperformance;

		private List<PriceData> _indexPriceData;
		private string _dataPointsPath;

		public void Run(int financialStatementCount, int lookAheadDays, decimal minOutperformance)
		{
			_financialStatementCount = financialStatementCount;
			_lookAheadDays = lookAheadDays;
			_minOutperformance = minOutperformance;
			_dataPointsPath = Path.Combine(Configuration.DataDirectory, Configuration.DataPointsPath);

			LoadIndex();
			var dataPoints = GetDataPoints();
			TrainAndEvaluateModel(dataPoints);
		}

		private void LoadIndex()
		{
			var indexTicker = CompanyTicker.GetIndexTicker();
			_indexPriceData = DataReader.GetPriceData(indexTicker);
		}

		private List<DataPoint> GetDataPoints()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var deserializedDataPoints = DeserializeDataPoints();
			if (deserializedDataPoints != null)
			{
				stopwatch.Stop();
				Console.WriteLine($"Loaded {deserializedDataPoints.Count} data points from \"{_dataPointsPath}\" in {stopwatch.Elapsed.TotalSeconds:F1} s");
				return deserializedDataPoints;
			}
			var tickers = DataReader.GetTickers();
			var dataPoints = new List<DataPoint>();
			int tickersProcessed = 0;
			int goodTickers = 0;
			Parallel.ForEach(tickers, ticker =>
			{
				tickersProcessed++;
				var financialStatements = DataReader.GetFinancialStatements(ticker);
				if (financialStatements == null)
				{
					Console.WriteLine($"Discarded {ticker.Ticker} due to lack of financial statements ({tickersProcessed}/{tickers.Count})");
					return;
				}
				var priceData = DataReader.GetPriceData(ticker);
				if (priceData == null)
				{
					Console.WriteLine($"Discarded {ticker.Ticker} due to lack of price data ({tickersProcessed}/{tickers.Count})");
					return;
				}
				GenerateDataPoints(financialStatements, priceData, dataPoints);
				goodTickers++;
				Console.WriteLine($"Generated data points for {ticker.Ticker} ({tickersProcessed}/{tickers.Count}), discarded {1.0m - (decimal)goodTickers / tickersProcessed:P1} of tickers");
			});
			SerializeDataPoints(dataPoints);
			stopwatch.Stop();
			Console.WriteLine($"Generated {dataPoints.Count} data points ({(decimal)dataPoints.Count / tickers.Count:F1} per ticker) in {stopwatch.Elapsed.TotalSeconds:F1} s, commencing training");
			return dataPoints;
		}

		private void SerializeDataPoints(List<DataPoint> dataPoints)
		{
			using (var fileStream = new FileStream(_dataPointsPath, FileMode.Create))
			{
				var serializer = MessagePackSerializer.Get<List<DataPoint>>();
				serializer.Pack(fileStream, dataPoints);
			}
		}

		private List<DataPoint> DeserializeDataPoints()
		{
			if (File.Exists(_dataPointsPath))
			{
				using (var fileStream = new FileStream(_dataPointsPath, FileMode.Open))
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
			var mlContext = new MLContext();
			const string FeatureName = "Features";
			var schema = SchemaDefinition.Create(typeof(DataPoint));
			int featureCount = dataPoints.First().Features.Length;
			schema[FeatureName].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
			var dataView = mlContext.Data.LoadFromEnumerable(dataPoints, schema);
			var splitData = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
			var options = new SdcaLogisticRegressionBinaryTrainer.Options
			{
				ConvergenceTolerance = 0.001f,
				BiasLearningRate = 0.1f,
				MaximumNumberOfIterations = 100
			};
			var estimator =
				mlContext.Transforms.NormalizeMinMax(FeatureName)
				.AppendCacheCheckpoint(mlContext)
				.Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(options));
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var model = estimator.Fit(splitData.TrainSet);
			stopwatch.Stop();
			Console.WriteLine($"Done training model in {stopwatch.Elapsed.TotalSeconds:F1} s");
			var predictions = model.Transform(splitData.TestSet);
			var metrics = mlContext.BinaryClassification.Evaluate(predictions);
			Console.WriteLine($"Accuracy: {metrics.Accuracy:P1}");
			Console.WriteLine($"AreaUnderRocCurve: {metrics.AreaUnderRocCurve:P1}");
			Console.WriteLine($"F1Score: {metrics.F1Score:P1}");
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
			for (int i = 0; i < financialStatements.Count - _financialStatementCount; i++)
			{
				var currentFinancialStatements = financialStatements.Skip(i).Take(_financialStatementCount).ToList();
				DateTime date1 = currentFinancialStatements.Last().SourceDate.Value;
				DateTime date2 = date1.AddDays(_lookAheadDays);
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
				decimal indexPerformance = indexPrice2.Value / indexPrice1.Value;
				decimal stockPerformance = stockPrice2.Value / stockPrice1.Value;
				decimal outperformance = stockPerformance - indexPerformance;
				var features = Features.GetFeatures(currentFinancialStatements);
				bool label = outperformance >= _minOutperformance;
				var dataPoint = new DataPoint
				{
					Features = features.ToArray(),
					Label = label
				};
				lock (dataPoints)
					dataPoints.Add(dataPoint);
			}
		}
	}
}
