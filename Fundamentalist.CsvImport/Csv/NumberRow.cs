using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvImport.Csv
{
	internal class NumberRow
	{
		[Name("adsh")]
		public string Adsh { get; set; }
		[Name("tag")]
		public string Tag { get; set; }
		[Name("ddate")]
		public int EndDate { get; set; }
		[Name("qtrs")]
		public int Quarters { get; set; }
		[Name("uom")]
		public string Unit { get; set; }
		[Name("value")]
		public decimal? Value { get; set; }
	}
}
