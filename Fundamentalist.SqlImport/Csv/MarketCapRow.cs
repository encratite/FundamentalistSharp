using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class MarketCapRow
	{
		[Name("symbol")]
		public string Symbol { get; set; }
		[Name("date")]
		public DateOnly Date { get; set; }
		[Name("market_cap")]
		public long MarketCap { get; set; }
	}
}
