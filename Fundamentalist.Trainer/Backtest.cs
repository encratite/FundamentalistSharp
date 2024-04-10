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
		private int _rebalanceDays;
		private float _minimumScore;
		private BacktestLoggingLevel _loggingLevel = BacktestLoggingLevel.None;

		public decimal IndexPerformance { get; set; }

		public Backtest
		(
			List<DataPoint> testData,
			List<PriceData> indexPriceData,
			decimal initialMoney = 100000.0m,
			decimal minimumInvestment = 50000.0m,
			decimal fee = 10.0m,
			int portfolioStocks = 10,
			int historyDays = 30,
			int rebalanceDays = 28,
			float minimumScore = 0.00f
		)
		{
			_testData = testData;
			_indexPriceData = indexPriceData;

			_initialMoney = initialMoney;
			_minimumInvestment = minimumInvestment;
			_fee = fee;
			_portfolioStocks = portfolioStocks;
			_historyDays = historyDays;
			_rebalanceDays = rebalanceDays;
			_minimumScore = minimumScore;
		}

		public decimal Run()
		{
			decimal money = _initialMoney;
			var portfolio = new List<Stock>();
			decimal feesPaid = 0.0m;

			DateTime initialDate = _testData.Take(2 * _portfolioStocks).Last().Date;
			DateTime now = initialDate;

			var log = (string message, BacktestLoggingLevel loggingLevel = BacktestLoggingLevel.All) =>
			{
				if (_loggingLevel >= loggingLevel)
					Console.WriteLine($"[{now.ToShortDateString()}] {message}");
			};

			log("Performing backtest");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var sellStocks = () =>
			{
				foreach (var stock in portfolio)
				{
					var priceData = stock.Data.PriceData;
					decimal? currentPrice = Trainer.GetPrice(now, priceData);
					if (currentPrice == null)
						currentPrice = priceData.Last().Mean;
					decimal ratio = currentPrice.Value / stock.BuyPrice;
					decimal change = ratio - 1.0m;
					decimal sellPrice = ratio * stock.InitialInvestment;
					decimal gain = change * stock.InitialInvestment;
					money += sellPrice - _fee;
					feesPaid += _fee;
					if (ratio > 1.0m)
						log($"Gained {gain:C0} ({change:+#.00%;-#.00%;+0.00%}) from selling {stock.Data.Ticker} (prediction {stock.Data.Score.Value:F3})");
					else
						log($"Lost {Math.Abs(gain):C0} ({change:+#.00%;-#.00%;+0.00%}) on {stock.Data.Ticker} (prediction {stock.Data.Score.Value:F3})");
				}
				portfolio.Clear();
			};

			DateTime finalTime = _testData.Last().Date + TimeSpan.FromDays(_historyDays);
			while (now.DayOfWeek != DayOfWeek.Monday)
				now += TimeSpan.FromDays(1);
			for (; now < finalTime; now += TimeSpan.FromDays(_rebalanceDays))
			{
				// Sell all stocks
				sellStocks();

				if (money < _minimumInvestment)
				{
					log("Ran out of money");
					break;
				}

				log($"Rebalancing portfolio with {money:C0} in the bank");

				var available =
					_testData.Where(x =>
						x.Date >= now - TimeSpan.FromDays(_historyDays) &&
						x.Date <= now &&
						x.Score.Value >= _minimumScore
					)
					.OrderByDescending(x => x.Score.Value)
					.ToList();
				if (!available.Any())
				{
					continue;
				}

				int count = Math.Min(_portfolioStocks, available.Count);
				if (count <= 0)
				{
					// There's nothing to buy
					continue;
				}

				// Buy new stocks
				decimal investment = (money - 2 * _portfolioStocks * _fee) / _portfolioStocks;
				foreach (var data in available)
				{
					if (portfolio.Any(x => x.Data.Ticker == data.Ticker))
					{
						// Make sure we don't accidentally buy the same stock twice
						continue;
					}
					decimal? currentPrice = Trainer.GetPrice(now, data.PriceData);
					if (currentPrice == null)
					{
						// Lacking price data
						continue;
					}
					var stock = new Stock
					{
						InitialInvestment = investment,
						BuyDate = now,
						BuyPrice = currentPrice.Value,
						Data = data
					};
					money -= investment + _fee;
					feesPaid += _fee;
					portfolio.Add(stock);
					if (portfolio.Count >= _portfolioStocks)
						break;
				}
			}
			// Cash out
			sellStocks();

			decimal? indexPast = Trainer.GetPrice(initialDate, _indexPriceData);
			decimal? indexNow = Trainer.GetPrice(now, _indexPriceData);
			if (indexNow == null)
				indexNow = _indexPriceData.Last().Mean;
			IndexPerformance = indexNow.Value / indexPast.Value - 1.0m;

			stopwatch.Stop();
			decimal performance = money / _initialMoney - 1.0m;
			log($"Finished backtest from {initialDate.ToShortDateString()} to {now.ToShortDateString()} with {money:C0} in the bank ({performance:+#.00%;-#.00%;+0.00%})", BacktestLoggingLevel.FinalOnly);
			log($"  portfolioStocks: {_portfolioStocks}", BacktestLoggingLevel.FinalOnly);
			log($"  historyDays: {_historyDays}", BacktestLoggingLevel.FinalOnly);
			log($"  rebalanceDays: {_rebalanceDays}", BacktestLoggingLevel.FinalOnly);
			log($"  minimumScore: {_minimumScore}", BacktestLoggingLevel.FinalOnly);
			log($"  feesPaid: {feesPaid:C}", BacktestLoggingLevel.FinalOnly);
			return performance;
		}
	}
}
