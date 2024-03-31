using Fundamentalist.Common;
using Fundamentalist.Common.Json.FinancialStatement;
using Microsoft.ML;
using System.Diagnostics;

namespace Fundamentalist.Trainer
{
	internal class Trainer
	{
		private int _financialStatementCount;
		private int _lookAheadDays;
		private decimal _minOutperformance;

		private List<PriceData> _indexPriceData;

		public void Run(int financialStatementCount, int lookAheadDays, decimal minOutperformance)
		{
			_financialStatementCount = financialStatementCount;
			_lookAheadDays = lookAheadDays;
			_minOutperformance = minOutperformance;

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var indexTicker = CompanyTicker.GetIndexTicker();
			_indexPriceData = DataReader.GetPriceData(indexTicker);
			var tickers = DataReader.GetTickers();
			var dataPoints = new List<DataPoint>();
			tickers = tickers.Take(20).ToList();
			int i = 1;
			foreach (var ticker in tickers)
			{
				var financialStatements = DataReader.GetFinancialStatements(ticker);
				if (financialStatements == null)
					continue;
				var priceData = DataReader.GetPriceData(ticker);
				if (priceData == null)
					continue;
				GenerateDataPoints(financialStatements, priceData, dataPoints);
				Console.WriteLine($"Generated data points for {ticker.Ticker} ({i}/{tickers.Count})");
				i++;
			}
			stopwatch.Stop();
			Console.WriteLine($"Generated {dataPoints.Count} data points in {stopwatch.Elapsed.TotalSeconds:F1} s, commencing training");
			var mlContext = new MLContext();
			var dataView = mlContext.Data.LoadFromEnumerable(dataPoints);
			var splitData = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
			var estimator =
				mlContext.Transforms.NormalizeMinMax("Features")
				.AppendCacheCheckpoint(mlContext)
				.Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());
			stopwatch.Reset();
			stopwatch.Start();
			var model = estimator.Fit(splitData.TrainSet);
			stopwatch.Stop();
			Console.WriteLine($"Processed training set in {stopwatch.Elapsed.TotalSeconds:F1} s");
			var predictions = model.Transform(splitData.TestSet);
			var metrics = mlContext.BinaryClassification.Evaluate(predictions);
			Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
			Console.WriteLine($"AreaUnderRocCurve: {metrics.AreaUnderRocCurve:P2}");
			Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
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
				dataPoints.Add(dataPoint);
			}
		}
	}
}
