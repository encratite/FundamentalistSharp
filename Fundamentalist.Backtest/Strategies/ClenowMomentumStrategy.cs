namespace Fundamentalist.Backtest.Strategies
{
	internal class ClenowMomentumStrategy : Strategy
	{
		private ClenowMomentumConfiguration _configuration;
		private DateTime? _lastRebalance = null;

		public ClenowMomentumStrategy(ClenowMomentumConfiguration configuration)
		{
			_configuration = configuration;
		}

		public override void Next()
		{
			bool rebalance =
				!_lastRebalance.HasValue ||
				Now - _lastRebalance.Value >= TimeSpan.FromDays(_configuration.RebalanceDays);
			if (!rebalance)
				return;
			var indexComponents = GetIndexComponents();
			int stockDays = Math.Max(_configuration.StockMovingAverageDays, _configuration.RegressionSlopeDays);
			stockDays = Math.Max(stockDays, _configuration.AverageTrueRangeDays + 1);
			DateTime to = Now;
			DateTime from = to.AddDays(- stockDays);
			var prices = GetPrices(indexComponents, from, to);
			var tickerRanking = new List<TickerPerformance>();
			foreach (var pair in prices)
			{
				var performance = new TickerPerformance(pair.Key, pair.Value, _configuration.RegressionSlopeDays, _configuration.StockMovingAverageDays);
				if (performance.AdjustedSlope.HasValue && performance.MovingAverage.HasValue)
					tickerRanking.Add(performance);
			}
			tickerRanking
				.Where(x => (double)x.GetLastClose() > x.MovingAverage)
				.OrderByDescending(x => x.AdjustedSlope)
				.Take(_configuration.IndexRankFilter)
				.ToList();
			_lastRebalance = Now;
			throw new NotImplementedException();
		}
	}
}
