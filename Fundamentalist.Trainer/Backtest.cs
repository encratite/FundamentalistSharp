using Fundamentalist.Common;
using System.Diagnostics;

namespace Fundamentalist.Trainer
{
	internal enum BacktestLoggingLevel
	{
		None,
		FinalOnly,
		All
	}

	internal class Backtest
	{
		private List<DataPoint> _testData;
		private List<PriceData> _indexPriceData;

		private decimal _initialMoney;
		private decimal _minimumInvestment;
		private decimal _fee;
		private int _portfolioStocks;
		private int _historyDays;
		private int _holdDays;
		private float _minimumScore;
		private BacktestLoggingLevel _loggingLevel = BacktestLoggingLevel.FinalOnly;

		public decimal IndexPerformance { get; set; }

		public Backtest
		(
			List<DataPoint> testData,
			List<PriceData> indexPriceData,
			decimal initialMoney = 100000.0m,
			decimal minimumInvestment = 10000.0m,
			decimal fee = 10.0m,
			int portfolioStocks = 10,
			int historyDays = 30,
			int holdDays = 30,
			float minimumScore = 0.08f
		)
		{
			_testData = testData;
			_indexPriceData = indexPriceData;

			_initialMoney = initialMoney;
			_minimumInvestment = minimumInvestment;
			_fee = fee;
			_portfolioStocks = portfolioStocks;
			_historyDays = historyDays;
			_holdDays = holdDays;
			_minimumScore = minimumScore;
		}

		public decimal Run()
		{
			decimal money = _initialMoney;
			var portfolio = new List<Stock>();

			DateTime initialDate = _testData.Take(2 * _portfolioStocks).Last().Date;
			DateTime now = initialDate;

			var log = (string message, BacktestLoggingLevel loggingLevel = BacktestLoggingLevel.All) =>
			{
				if (_loggingLevel >= loggingLevel)
					Console.WriteLine($"[{now.ToShortDateString()}] {message}");
			};

			log("Performing backtest", BacktestLoggingLevel.All);
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var sellStocks = (bool sellAll, bool sellExpired) =>
			{
				foreach (var stock in portfolio.ToList())
				{
					bool hasExpired = now - stock.BuyDate > TimeSpan.FromDays(_holdDays);
					bool sellStock = sellAll || (sellExpired && hasExpired);
					if (!sellStock)
						continue;
					var priceData = stock.Data.PriceData;
					decimal? currentPrice = Trainer.GetPrice(now, priceData);
					if (currentPrice == null)
						currentPrice = priceData.Last().Mean;
					decimal ratio = currentPrice.Value / stock.BuyPrice;
					decimal change = ratio - 1.0m;
					decimal sellPrice = ratio * stock.InitialInvestment;
					decimal gain = change * stock.InitialInvestment;
					money += sellPrice - _fee;
					if (ratio > 1.0m)
						log($"Gained {gain:C0} ({change:+#.00%;-#.00%;+0.00%}) from selling {stock.Data.Ticker} (prediction {stock.Data.Score.Value:F3})");
					else
						log($"Lost {Math.Abs(gain):C0} ({change:+#.00%;-#.00%;+0.00%}) on {stock.Data.Ticker} (prediction {stock.Data.Score.Value:F3})");
					portfolio.Remove(stock);
				}
			};

			foreach (var currentDataPoint in _testData)
			{
				now = currentDataPoint.Date;

				// Sell expired stocks
				sellStocks(false, true);

				if (money < 0)
				{
					log("Ran out of money");
					break;
				}
				else if (money < _minimumInvestment)
				{
					// Not enough money to rebalance
					continue;
				}

				log($"Rebalancing portfolio with {money:C0} in the bank");

				var available =
					_testData.Where(x =>
						x.Date >= now - TimeSpan.FromDays(_historyDays) &&
						x.Date <= now &&
						x.Score.Value >= _minimumScore &&
						!portfolio.Any(x => x.Data.Ticker == currentDataPoint.Ticker)
					)
					.OrderByDescending(x => x.Score.Value)
					.ToList();
				if (!available.Any())
				{
					continue;
				}

				int count = Math.Min(_portfolioStocks - portfolio.Count, available.Count);
				if (count <= 0)
				{
					// There's nothing to buy
					continue;
				}

				// Buy new stocks
				decimal investment = (money - 2 * count * _fee) / count;
				foreach (var data in available)
				{
					if (portfolio.Any(x => x.Data.Ticker == data.Ticker))
					{
						// Make sure we don't accidentally buy the same stock twice
						continue;
					}
					decimal currentPrice = Trainer.GetPrice(now, data.PriceData).Value;
					var stock = new Stock
					{
						InitialInvestment = investment,
						BuyDate = now,
						BuyPrice = currentPrice,
						Data = data
					};
					money -= investment + _fee;
					portfolio.Add(stock);
					if (portfolio.Count >= _portfolioStocks)
						break;
				}
			}
			// Cash out
			sellStocks(true, false);

			decimal? indexPast = Trainer.GetPrice(initialDate, _indexPriceData);
			decimal? indexNow = Trainer.GetPrice(now, _indexPriceData);
			if (indexNow == null)
				indexNow = _indexPriceData.Last().Mean;
			IndexPerformance = indexNow.Value / indexPast.Value - 1.0m;

			stopwatch.Stop();
			decimal performance = money / _initialMoney - 1.0m;
			if (_loggingLevel >= BacktestLoggingLevel.FinalOnly)
			{
				Console.WriteLine($"Finished backtest from {initialDate.ToShortDateString()} to {now.ToShortDateString()} with {money:C0} in the bank ({performance:+#.00%;-#.00%;+0.00%})");
				// Console.WriteLine($"S&P 500 performance during that time: {IndexPerformance:+#.00%;-#.00%;+0.00%}");
				Console.WriteLine($"  portfolioStocks: {_portfolioStocks}");
				Console.WriteLine($"  historyDays: {_historyDays}");
			}
			return performance;
		}
	}
}
