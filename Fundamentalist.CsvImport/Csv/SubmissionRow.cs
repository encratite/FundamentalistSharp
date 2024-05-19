using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class SubmissionRow
	{
		[Name("adsh")]
		public string Adsh { get; set; }
		[Name("cik")]
		public int Cik { get; set; }
		[Name("form")]
		public string Form { get; set; }
		[Name("period")]
		public int? Period { get; set; }
		[Name("fy")]
		public string FiscalYear { get; set; }
		[Name("fp")]
		public string FiscalPeriod { get; set; }
		[Name("filed")]
		public int Filed { get; set; }
		[Name("accepted")]
		public DateTime Accepted { get; set; }
	}
}
