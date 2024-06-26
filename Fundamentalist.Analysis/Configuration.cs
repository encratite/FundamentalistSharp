﻿namespace Fundamentalist.Analysis
{
	internal class Configuration
	{
		public string ConnectionString { get; set; }
		public string Form { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public int? Horizon { get; set; }
		public decimal? MinimumFrequency { get; set; }
		public int? MinimumMarketCap { get; set; }
		public int? MaximumMarketCap { get; set; }
		public string Output { get; set; }

		public void Validate()
		{
			bool valid =
				ConnectionString != null &&
				Form != null &&
				From.HasValue &&
				To.HasValue &&
				Horizon.HasValue &&
				MinimumFrequency.HasValue &&
				MinimumMarketCap.HasValue &&
				MaximumMarketCap.HasValue &&
				Output != null;
			if (!valid)
				throw new ApplicationException("Invalid configuration file");
		}
	}
}
