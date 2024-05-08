namespace Fundamentalist.SqlAnalysis
{
	internal class Configuration
	{
		public string ConnectionString { get; set; }
		public string Form { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public decimal? Upper { get; set; }
		public decimal? Lower { get; set; }
		public int? Limit { get; set; }
		public decimal? MinimumFrequency { get; set; }
		public string Output { get; set; }

		public void Validate()
		{
			bool valid =
				ConnectionString != null &&
				Form != null &&
				From.HasValue &&
				To.HasValue &&
				Upper.HasValue &&
				Lower.HasValue &&
				Limit.HasValue &&
				MinimumFrequency.HasValue &&
				Output != null;
			if (!valid)
				throw new ApplicationException("Invalid configuration file");
		}
	}
}
