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
		private SortedList<DateTime, PriceData> _indexPriceData;

		private decimal _initialMoney;
		private decimal _minimumInvestment;
		private int _portfolioStocks;
		private int _historyDays;
		private int _rebalanceDays;
		private float _minimumGain;
		private decimal _minimumStockPrice;
		private BacktestLoggingLevel _loggingLevel = BacktestLoggingLevel.None;

		public decimal IndexPerformance { get; set; }

		public Backtest
		(
			List<DataPoint> testData,
			SortedList<DateTime, PriceData> indexPriceData,
			decimal initialMoney = 100000.0m,
			decimal minimumInvestment = 50000.0m,
			int portfolioStocks = 5,
			int historyDays = 7,
			int rebalanceDays = 7,
			float minimumGain = 0.05f,
			decimal minimumStockPrice = 1.0m
		)
		{
			_testData = testData;
			_indexPriceData = indexPriceData;

			_initialMoney = initialMoney;
			_minimumInvestment = minimumInvestment;

			_portfolioStocks = portfolioStocks;
			_historyDays = historyDays;
			_rebalanceDays = rebalanceDays;
			_minimumGain = minimumGain;
			_minimumStockPrice = minimumStockPrice;
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
					decimal? currentPrice = Trainer.GetOpenPrice(now, priceData);
					if (currentPrice == null)
						currentPrice = priceData.Values.Last().Close;
					decimal ratio = currentPrice.Value / stock.BuyPrice;
					decimal change = ratio - 1.0m;
					decimal fees = GetTransactionFees(stock.Count, currentPrice.Value, true);
					decimal sellPrice = stock.Count * currentPrice.Value;
					decimal gain = change * sellPrice;
					money += sellPrice - fees;
					feesPaid += fees;
					if (ratio > 1.0m)
						log($"Gained {gain:C0} ({change:+#.00%;-#.00%;+0.00%}) from selling {stock.Data.Ticker} ({currentPrice.Value:C2} vs. predicted {stock.Data.Score.Value:C2})");
					else
						log($"Lost {-gain:C0} ({change:+#.00%;-#.00%;+0.00%}) on {stock.Data.Ticker} ({currentPrice.Value:C2} vs. predicted {stock.Data.Score.Value:C2})");
				}
				portfolio.Clear();
			};

			DateTime finalTime = _testData.Last().Date + TimeSpan.FromDays(_historyDays);
			while (now.DayOfWeek != DayOfWeek.Friday)
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

				var inRange =
					_testData.Where(x =>
						x.Date >= now - TimeSpan.FromDays(_historyDays) &&
						x.Date <= now
					);
				var available = new List<Tuple<DataPoint, float>>();
				foreach (var x in inRange)
				{
					decimal? currentPrice = Trainer.GetOpenPrice(now, x.PriceData);
					if (currentPrice == null || currentPrice.Value < _minimumStockPrice)
						continue;
					float predictedChange = x.Score.Value / (float)currentPrice.Value - 1.0f;
					// log($"Predicted gain for {x.Ticker}: {predictedChange:#.00%}");
					if (predictedChange >= _minimumGain)
						available.Add(new Tuple<DataPoint, float>(x, predictedChange));
				}
				if (!available.Any())
					continue;
				available = available.OrderByDescending(x => x.Item2).ToList();

				int availableCount = Math.Min(_portfolioStocks, available.Count);
				if (availableCount <= 0)
				{
					// There's nothing to buy
					continue;
				}

				const decimal EstimatedFees = 10.0m;
				const decimal Spread = 0.004m;

				// Buy new stocks
				decimal investment = (money - 2 * _portfolioStocks * EstimatedFees) / _portfolioStocks;
				foreach (var data in available.Select(x => x.Item1))
				{
					if (portfolio.Any(x => x.Data.Ticker == data.Ticker))
					{
						// Make sure we don't accidentally buy the same stock twice
						continue;
					}
					decimal? currentPrice = Trainer.GetOpenPrice(now, data.PriceData);
					if (currentPrice == null)
					{
						// Lacking price data
						continue;
					}
					// Simulate spread
					currentPrice *= 1.0m + Spread;
					int count = (int)Math.Floor(investment / currentPrice.Value);
					var stock = new Stock
					{
						Count = count,
						BuyDate = now,
						BuyPrice = currentPrice.Value,
						Data = data
					};
					decimal fees = GetTransactionFees(count, currentPrice.Value, false);
					money -= count * currentPrice.Value + fees;
					feesPaid += fees;
					portfolio.Add(stock);
					if (portfolio.Count >= _portfolioStocks)
						break;
				}
			}
			// Cash out
			sellStocks();

			decimal? indexPast = Trainer.GetClosePrice(initialDate, _indexPriceData);
			decimal? indexNow = Trainer.GetClosePrice(now, _indexPriceData);
			if (indexNow == null)
				indexNow = _indexPriceData.Values.Last().Close;
			IndexPerformance = indexNow.Value / indexPast.Value - 1.0m;

			stopwatch.Stop();
			decimal performance = money / _initialMoney - 1.0m;
			log($"Finished backtest from {initialDate.ToShortDateString()} to {now.ToShortDateString()} with {money:C0} in the bank ({performance:+#.00%;-#.00%;+0.00%})", BacktestLoggingLevel.FinalOnly);
			log($"  portfolioStocks: {_portfolioStocks}", BacktestLoggingLevel.FinalOnly);
			log($"  historyDays: {_historyDays}", BacktestLoggingLevel.FinalOnly);
			log($"  rebalanceDays: {_rebalanceDays}", BacktestLoggingLevel.FinalOnly);
			log($"  minimumScore: {_minimumGain}", BacktestLoggingLevel.FinalOnly);
			// log($"  feesPaid: {feesPaid:C}", BacktestLoggingLevel.FinalOnly);
			return performance;
		}

		decimal GetTransactionFees(int count, decimal price, bool selling)
		{
			return 0;
		}
	}
}
