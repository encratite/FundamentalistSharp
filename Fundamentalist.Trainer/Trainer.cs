using Fundamentalist.Common;
using Fundamentalist.Common.Json.FinancialStatement;
using Microsoft.ML;
using System.Diagnostics;

namespace Fundamentalist.Trainer
{
	internal class Trainer
	{
		public void Run()
		{
			var indexTicker = CompanyTicker.GetIndexTicker();
			var indexPriceData = DataReader.GetPriceData(indexTicker);
			var tickers = DataReader.GetTickers();
			foreach (var ticker in tickers)
			{
				var financialStatements = DataReader.GetFinancialStatements(ticker);
				if (financialStatements == null)
					continue;
				var priceData = DataReader.GetPriceData(ticker);
				if (priceData == null)
					continue;
			}
			var context = new MLContext();
			// context.Data.LoadFromEnumerable
			throw new NotImplementedException();
		}

		public void AnalyzeData()
		{
			const decimal Limit = 0.9m;

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			int companyCount = 0;
			int financialStatementCount = 0;
			var results = new Dictionary<string, int>();
			var tickers = DataReader.GetTickers();
			FeatureName[] lastFeatureNames = null;
			foreach (var ticker in tickers)
			{
				var financialStatements = DataReader.GetFinancialStatements(ticker);
				if (financialStatements == null)
					continue;
				if (financialStatements.Any())
					companyCount++;
				foreach (var financialStatement in financialStatements)
				{
					var featureNames = FinancialStatement.GetFeatureNames(financialStatement);
					foreach (var featureName in featureNames)
					{
						string key = featureName.Name;
						if (!results.ContainsKey(key))
							results[key] = 0;
						if (featureName.HasValue)
							results[featureName.Name]++;
					}
					financialStatementCount++;
					lastFeatureNames = featureNames;
				}
				// Console.WriteLine($"Analyzed {ticker.Ticker}");
			}
			foreach (var featureName in lastFeatureNames)
			{
				decimal ratio = (decimal)results[featureName.Name] / financialStatementCount;
				bool belowLimit = ratio < Limit;
				string message = $"{featureName.Name}: {ratio:P}";
				if (belowLimit)
					Utility.WriteError(message);
				else
					Console.WriteLine(message);
			}
			stopwatch.Stop();
			Console.WriteLine($"Evaluated {financialStatementCount} financial statements from {companyCount} companies in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
		}
	}
}
