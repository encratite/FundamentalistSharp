using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class TickerRow
	{
		[Name("symbol")]
		public string Symbol { get; set; }
		[Name("cik")]
		public int Cik { get; set; }
		[Name("company")]
		public string Company { get; set; }
		[Name("sector")]
		public string Sector { get; set; }
		[Name("industry")]
		public string Industry { get; set; }
		[Name("exclude")]
		public int Exclude { get; set; }
	}
}
