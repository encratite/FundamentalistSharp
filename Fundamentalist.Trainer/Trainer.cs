using Fundamentalist.Common;
using Microsoft.ML;

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
				var features = financialStatements[0].GetFeatures();
			}
			var context = new MLContext();
			// context.Data.LoadFromEnumerable
			throw new NotImplementedException();
		}
	}
}
