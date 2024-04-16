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
		private const decimal Spread = 0.004m;

		private List<DataPoint> _testData;
		private SortedList<DateTime, PriceData> _indexPriceData;

		private decimal _initialCapital;
		private decimal _investment;
		private int _holdDays;
		private float _minimumGain;
		private decimal _minimumStockPrice;
		private decimal _minimumVolume;
		private BacktestLoggingLevel _loggingLevel = BacktestLoggingLevel.None;

		public decimal IndexPerformance { get; set; }

		public Backtest
		(
			List<DataPoint> testData,
			SortedList<DateTime, PriceData> indexPriceData,
			decimal initialCapital = 100000.0m,
			decimal investment = 25000.0m,
			int holdDays = 7,
			float minimumGain = 0.05f,
			decimal minimumStockPrice = 1.0m,
			decimal minimumVolume = 1e6m
		)
		{
			_testData = testData;
			_indexPriceData = indexPriceData;

			_initialCapital = initialCapital;
			_investment = investment;
			_holdDays = holdDays;
			_minimumGain = minimumGain;
			_minimumStockPrice = minimumStockPrice;
			_minimumVolume = minimumVolume;
		}

		public decimal Run()
		{
			decimal money = _initialCapital;
			var portfolio = new List<Stock>();
			decimal feesPaid = 0.0m;

			DateTime initialDate = _testData.First().Date;
			DateTime now = initialDate;

			var log = (string message, BacktestLoggingLevel loggingLevel = BacktestLoggingLevel.All) =>
			{
				if (_loggingLevel >= loggingLevel)
					Console.WriteLine($"[{now.ToShortDateString()}] {message}");
			};

			log("Performing backtest");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var sellStocks = (bool sellExpired) =>
			{
				foreach (var stock in portfolio.ToList())
				{
					if (sellExpired && now - stock.BuyDate < TimeSpan.FromDays(_holdDays))
						continue;
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
					portfolio.Remove(stock);
				}
			};

			DateTime finalTime = _testData.Last().Date + TimeSpan.FromDays(_holdDays);
			for (; now < finalTime; now += TimeSpan.FromDays(1))
			{
				if (
					now.DayOfWeek == DayOfWeek.Saturday ||
					now.DayOfWeek == DayOfWeek.Sunday
				)
					continue;
				sellStocks(true);
				foreach (var dataPoint in _testData.Where(x => x.Date == now - TimeSpan.FromDays(1)))
				{
					if (money < _investment)
						break;
					if (dataPoint.Score.Value == float.NaN)
						continue;
					decimal? currentPrice = Trainer.GetOpenPrice(now, dataPoint.PriceData);
					if (currentPrice == null || currentPrice.Value < _minimumStockPrice)
						continue;
					decimal volume = dataPoint.PriceData[now].Volume * currentPrice.Value;
					if (volume < _minimumVolume)
						continue;
					float predictedChange = dataPoint.Score.Value / (float)currentPrice.Value - 1.0f;
					if (predictedChange < _minimumGain)
						continue;

					// Simulate spread
					currentPrice *= 1.0m + Spread;
					int count = (int)Math.Floor(_investment / currentPrice.Value);
					var stock = new Stock
					{
						Count = count,
						BuyDate = now,
						BuyPrice = currentPrice.Value,
						Data = dataPoint
					};
					decimal fees = GetTransactionFees(count, currentPrice.Value, false);
					money -= count * currentPrice.Value + fees;
					feesPaid += fees;
					portfolio.Add(stock);
				}
			}
			// Cash out
			sellStocks(false);

			decimal? indexPast = Trainer.GetClosePrice(initialDate, _indexPriceData);
			decimal? indexNow = Trainer.GetClosePrice(now, _indexPriceData);
			if (indexNow == null)
				indexNow = _indexPriceData.Values.Last().Close;
			IndexPerformance = indexNow.Value / indexPast.Value - 1.0m;

			stopwatch.Stop();
			decimal performance = money / _initialCapital - 1.0m;
			log($"Finished backtest from {initialDate.ToShortDateString()} to {now.ToShortDateString()} with {money:C0} in the bank ({performance:+#.00%;-#.00%;+0.00%})", BacktestLoggingLevel.FinalOnly);
			log($"  holdDays: {_holdDays}", BacktestLoggingLevel.FinalOnly);
			log($"  minimumGain: {_minimumGain}", BacktestLoggingLevel.FinalOnly);
			// log($"  feesPaid: {feesPaid:C}", BacktestLoggingLevel.FinalOnly);
			return performance;
		}

		decimal GetTransactionFees(int count, decimal price, bool selling)
		{
			return 0;
		}
	}
}
