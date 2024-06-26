﻿namespace Fundamentalist.Backtest.Strategies
{
	public class ClenowMomentumConfiguration
	{
		public int SellDays { get; set; } = 7;
		public int RebalanceDays { get; set; } = 14;
		public int IndexMovingAverageDays { get; set; } = 200;
		public int IndexRankFilter { get; set; } = 100;
		public int StockMovingAverageDays { get; set; } = 100;
		public int RegressionSlopeDays { get; set; } = 90;
		public int AverageTrueRangeDays { get; set; } = 20;
		public decimal GapFilter { get; set; } = 0.15m;
		public decimal RiskFactor { get; set; } = 0.001m;
		public decimal MinimumPositionSize { get; set; } = 1000m;
	}
}
