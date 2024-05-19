using CsvHelper.Configuration.Attributes;
using Fundamentalist.CsvImport.Document;

namespace Fundamentalist.CsvImport.Csv
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

		public SecSubmission GetSecSubmission()
		{
			var output = new SecSubmission
			{
				Adsh = Adsh,
				Cik = Cik,
				Form = Form,
				Period = IntDate.Get(Period),
				FiscalPeriod = FiscalPeriod,
				Filed = IntDate.Get(Filed).Value,
				Accepted = Accepted
			};
			int fiscalYear;
			if (int.TryParse(FiscalYear, out fiscalYear))
				output.FiscalYear = fiscalYear;
			return output;
		}
	}
}
