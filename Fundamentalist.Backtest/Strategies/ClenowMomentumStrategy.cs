﻿namespace Fundamentalist.Backtest.Strategies
{
	internal class ClenowMomentumStrategy : Strategy
	{
		private ClenowMomentumConfiguration _configuration;
		private DateTime? _lastSellCheck = null;
		private DateTime? _lastRebalance = null;

		public ClenowMomentumStrategy(ClenowMomentumConfiguration configuration)
		{
			_configuration = configuration;
		}

		public override void Next()
		{
			bool sellCheck = TimeExceeded(_configuration.SellDays, ref _lastSellCheck);
			if (!sellCheck)
				return;
			var tickerRanking = GetTickerRanking();
			foreach (var position in Positions.Values)
			{
				if (!tickerRanking.Any(x => x.Ticker == position.Ticker))
					Sell(position.Ticker, position.Count);
			}
			bool rebalance = TimeExceeded(_configuration.RebalanceDays, ref _lastRebalance);
			if (!rebalance)
				return;
			decimal accountValue = GetAccountValue();
			Console.WriteLine($"[{Now.ToShortDateString()}] Rebalancing {accountValue:C}");
			Rebalance(true, accountValue, tickerRanking);
			Rebalance(false, accountValue, tickerRanking);
			if (!IsBullMarket())
				return;
			BuyStocks(accountValue, tickerRanking);
		}

		private bool TimeExceeded(int days, ref DateTime? date)
		{
			bool exceeded =
				!date.HasValue ||
				Now - date.Value >= TimeSpan.FromDays(days);
			if (exceeded)
				date = Now;
			return exceeded;
		}

		private List<TickerPerformance> GetTickerRanking()
		{
			var indexComponents = GetIndexComponents();
			int stockDays = Math.Max(_configuration.StockMovingAverageDays, _configuration.RegressionSlopeDays);
			stockDays = Math.Max(stockDays, _configuration.AverageTrueRangeDays + 1);
			var prices = GetPrices(indexComponents, Now, stockDays);
			var tickerRanking = new List<TickerPerformance>();
			foreach (var pair in prices)
			{
				var performance = new TickerPerformance(pair.Key, pair.Value, _configuration);
				if (performance.AdjustedSlope.HasValue && performance.MovingAverage.HasValue)
					tickerRanking.Add(performance);
			}
			tickerRanking = tickerRanking
				.Where(x => (double)x.GetLastClose() > x.MovingAverage)
				.OrderByDescending(x => x.AdjustedSlope)
				.Take(_configuration.IndexRankFilter)
				.ToList();
			return tickerRanking;
		}

		private decimal GetAccountValue()
		{
			decimal accountValue = Cash;
			foreach (var position in Positions.Values)
			{
				decimal? price = GetOpenPrice(position.Ticker, Now);
				if (!price.HasValue)
					continue;
				accountValue += position.Count * price.Value;
			}
			return accountValue;
		}

		private void Rebalance(bool sell, decimal accountValue, List<TickerPerformance> tickerRanking)
		{
			foreach (var position in Positions.Values)
			{
				var ranking = tickerRanking.First(x => x.Ticker == position.Ticker);
				long shares = GetPositionShares(accountValue, ranking);
				long difference = shares - position.Count;
				if (sell && difference < 0)
					Sell(position.Ticker, -difference);
				else if (!sell && difference > 0)
					Buy(position.Ticker, difference);
			}
		}

		private long GetPositionShares(decimal accountValue, TickerPerformance ranking)
		{
			long shares = (long)Math.Round(accountValue * _configuration.RiskFactor / ranking.AverageTrueRange);
			return shares;
		}

		private bool IsBullMarket()
		{
			var indexPrices = GetPrices((string)null, Now, _configuration.IndexMovingAverageDays);
			decimal sum = 0;
			foreach (var price in indexPrices)
				sum += price.Close;
			decimal movingAverage = sum / indexPrices.Count;
			decimal finalClose = indexPrices.Last().Close;
			bool isBullMarket = finalClose > movingAverage;
			return isBullMarket;
		}

		private void BuyStocks(decimal accountValue, List<TickerPerformance> tickerRanking)
		{
			foreach (var ranking in tickerRanking)
			{
				if (Positions.ContainsKey(ranking.Ticker))
					continue;
				long shares = GetPositionShares(accountValue, ranking);
				if (shares == 0)
					break;
				var tickerData = GetTickerData(ranking.Ticker);
				if (
					tickerData == null ||
					tickerData.Industry == "Telecom Services" ||
					tickerData.Sector == "Utilities"
				)
					continue;
				if (!Buy(ranking.Ticker, shares))
					break;
			}
		}
	}
}
