namespace Fundamentalist.Backtest
{
	internal class Configuration
	{
		public string ConnectionString { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public decimal? Cash { get; set; }
		public decimal? Spread { get; set; }

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
				Spread >= 0;
			if (!valid)
				throw new ApplicationException("Invalid configuration file");
		}
	}
}
