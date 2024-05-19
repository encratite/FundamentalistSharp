using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvImport.Csv
{
	internal class LegacyPriceRow
	{
		[Name("Date")]
		public DateTime Date { get; set; }
		[Name("Open")]
		public decimal Open { get; set; }
		[Name("High")]
		public decimal High { get; set; }
		[Name("Low")]
		public decimal Low { get; set; }
		[Name("Close")]
		public decimal Close { get; set; }
		[Name("Adj Close")]
		public decimal AdjustedClose { get; set; }
		[Name("Volume")]
		public long Volume { get; set; }
	}
}
