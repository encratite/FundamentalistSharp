using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvImport.Csv
{
	internal class TickerRow
	{
		[Name("ticker")]
		public string Ticker { get; set; }
		[Name("name")]
		public string Name { get; set; }
		[Name("isdelisted")]
		public string IsDelisted { get; set; }
		[Name("category")]
		public string Category { get; set; }
		[Name("sicsector")]
		public string SicSector { get; set; }
		[Name("sicindustry")]
		public string SicIndustry { get; set; }
		[Name("famaindustry")]
		public string FamaIndustry { get; set; }
		[Name("sector")]
		public string Sector { get; set; }
		[Name("industry")]
		public string Industry { get; set; }
		[Name("scalemarketcap")]
		public string MarketCap { get; set; }
		[Name("scalerevenue")]
		public string Revenue { get; set; }
		[Name("relatedtickers")]
		public string RelatedTickers { get; set; }
		[Name("currency")]
		public string Currency { get; set; }
		[Name("location")]
		public string Location { get; set; }
		[Name("secfilings")]
		public string SecFilings { get; set; }
	}
}