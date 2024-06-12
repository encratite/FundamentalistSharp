namespace Fundamentalist.Backtest
{
	public class Configuration
	{
		public string ConnectionString { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public decimal? Cash { get; set; }
		public decimal? Spread { get; set; }
		public decimal? FeesPerShare { get; set; }
		public decimal? MinimumFeesPerOrder { get; set; }
		public decimal? MaximumFeesPerOrderRatio { get; set; }

		public void Validate()
		{
			bool valid =
				ConnectionString != null &&
				From.HasValue &&
				To.HasValue &&
				From < To &&
				Cash.HasValue &&
				Cash > 0 &&
				Spread.HasValue &&
				Spread >= 0 &&
				FeesPerShare.HasValue &&
				FeesPerShare > 0 &&
				MinimumFeesPerOrder.HasValue &&
				MinimumFeesPerOrder > 0 &&
				MaximumFeesPerOrderRatio.HasValue;
			if (!valid)
				throw new ApplicationException("Invalid configuration file");
		}
	}
}
