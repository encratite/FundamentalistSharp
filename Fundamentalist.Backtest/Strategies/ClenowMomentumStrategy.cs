namespace Fundamentalist.Backtest.Strategies
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
			Rebalance(true, tickerRanking, accountValue);
			Rebalance(false, tickerRanking, accountValue);
			if (!IsBullMarket())
				return;
			throw new NotImplementedException();
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
			DateTime to = Now;
			DateTime from = to.AddDays(-stockDays);
			var prices = GetPrices(indexComponents, from, to);
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
			decimal accountValue = 0;
			foreach (var position in Positions.Values)
			{
				decimal? price = GetOpenPrice(position.Ticker, Now);
				if (!price.HasValue)
					continue;
				accountValue += position.Count * price.Value;
			}
			return accountValue;
		}

		private void Rebalance(bool sell, List<TickerPerformance> tickerRanking, decimal accountValue)
		{
			foreach (var position in Positions.Values)
			{
				var ranking = tickerRanking.First(x => x.Ticker == position.Ticker);
				long shares = (long)Math.Round(accountValue * _configuration.RiskFactor / ranking.AverageTrueRange);
				long difference = shares - position.Count;
				if (sell && difference < 0)
					Sell(position.Ticker, -difference);
				else if (!sell && difference > 0)
					Buy(position.Ticker, difference);
			}
		}

		private bool IsBullMarket()
		{
			var indexPrices = GetPrices((string)null, Now.AddDays(-_configuration.IndexMovingAverageDays), Now);
			throw new NotImplementedException();
		}
	}
}
