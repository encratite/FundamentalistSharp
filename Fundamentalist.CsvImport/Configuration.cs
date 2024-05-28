namespace Fundamentalist.CsvImport
{
	internal class Configuration
	{
		public string EdgarPath { get; set; }
		public string PriceCsvPath { get; set; }
		public string IndexPriceCsvPath { get; set; }
		public string IndexComponentsCsvPath { get; set; }
		public string TickerCsvPath { get; set; }
		public string ConnectionString { get; set; }

		public void Validate()
		{
			bool valid =
				EdgarPath != null &&
				PriceCsvPath != null &&
				IndexPriceCsvPath != null &&
				IndexComponentsCsvPath != null &&
				TickerCsvPath != null;
			if (!valid)
				throw new ApplicationException("Invalid configuration file");
		}
	}
}
