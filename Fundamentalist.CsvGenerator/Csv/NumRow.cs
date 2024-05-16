using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class NumRow
	{
		[Name("adsh")]
		public string AccessionNumber { get; set; }
		[Name("tag")]
		public string Tag { get; set; }
		[Name("ddate")]
		public int EndDateInt { get; set; }
		[Name("qtrs")]
		public int Quarters { get; set; }
		[Name("uom")]
		public string Unit { get; set; }
		[Name("value")]
		public decimal Value { get; set; }
	}
}
