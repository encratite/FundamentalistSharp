using Fundamentalist.CsvGenerator.Csv;
using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.CsvGenerator.Document
{
	internal class SecSubmission
	{
		[BsonId]
		public string Adsh { get; set; }
		public string Form { get; set; }
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime? Period { get; set; }
		public int? FiscalYear { get; set; }
		public string FiscalPeriod { get; set; }
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime Filed { get; set; }
		public DateTime Accepted { get; set; }
		public List<SecNumber> Numbers { get; set; } = new List<SecNumber>();

		public SecSubmission(SubmissionRow submission)
		{
			Adsh = submission.Adsh;
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
