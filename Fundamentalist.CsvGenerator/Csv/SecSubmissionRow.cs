using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class SecSubmissionRow
	{
		[Name("adsh")]
		public string Adsh { get; set; }
		[Name("cik")]
		public int Cik { get; set; }
		[Name("form")]
		public string Form { get; set; }
		[Name("period")]
		public DateOnly? Period { get; set; }
		[Name("fiscal_year")]
		public int? FiscalYear { get; set; }
		[Name("fiscal_period")]
		public string FiscalPeriod { get; set; }
		[Name("filed")]
		public DateOnly Filed { get; set; }
		[Name("accepted")]
		public DateTime Accepted { get; set; }

		public SecSubmissionRow(SubmissionRow submission)
		{
			Adsh = submission.Adsh;
			Cik = submission.Cik;
			Form = submission.Form;
			Period = IntDate.Get(submission.Period);
			int fiscalYear;
			if (int.TryParse(submission.FiscalYear, out fiscalYear))
				FiscalYear = fiscalYear;
			FiscalPeriod = submission.FiscalPeriod;
			Filed = IntDate.Get(submission.Filed).Value;
			Accepted = submission.Accepted;
		}
	}
}
