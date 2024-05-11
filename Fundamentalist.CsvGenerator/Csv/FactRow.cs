using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class FactRow
	{
		[Name("cik")]
		public int Cik { get; set; }
		[Name("name")]
		public string Name { get; set; }
		[Name("unit")]
		public string Unit { get; set; }
		[Name("start_date")]
		public DateOnly Start { get; set; }
		[Name("end_date")]
		public DateOnly End { get; set; }
		[Name("value")]
		public decimal Value { get; set; }
		[Name("fiscal_year")]
		public int? FiscalYear { get; set; }
		[Name("fiscal_period")]
		public string FiscalPeriod { get; set; }
		[Name("form")]
		public string Form { get; set; }
		[Name("filed")]
		public DateOnly Filed { get; set; }
		[Name("frame")]
		public string Frame { get; set; }
	}
}
