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

			_lastRebalance = Now;
		}
	}
}
