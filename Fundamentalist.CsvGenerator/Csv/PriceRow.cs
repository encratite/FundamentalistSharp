using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class PriceRow
	{
		[Name("symbol")]
		public string Symbol { get; set; }
		[Name("date")]
		public DateOnly Date { get; set; }
		[Name("open_price")]
		public decimal Open { get; set; }
		[Name("high")]
		public decimal High { get; set; }
		[Name("low")]
		public decimal Low { get; set; }
		[Name("close_price")]
		public decimal Close { get; set; }
		[Name("adjusted_close")]
		public decimal AdjustedClose { get; set; }
		[Name("volume")]
		public long Volume { get; set; }
	}
}
