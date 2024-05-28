namespace Fundamentalist.Backtest
{
	internal class Configuration
	{
		public string ConnectionString { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }

		public void Validate()
		{
			bool valid =
				ConnectionString != null &&
				From.HasValue &&
				To.HasValue;
			if (!valid)
				throw new ApplicationException("Invalid configuration file");
		}
	}
}
