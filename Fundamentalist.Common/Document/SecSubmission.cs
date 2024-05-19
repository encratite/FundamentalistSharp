using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.CsvImport.Document
{
	public class SecSubmission
	{
		[BsonId]
		public string Adsh { get; set; }
		public int Cik { get; set; }
		public string Form { get; set; }
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime? Period { get; set; }
		public int? FiscalYear { get; set; }
		public string FiscalPeriod { get; set; }
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime Filed { get; set; }
		public DateTime Accepted { get; set; }
		public List<SecNumber> Numbers { get; set; } = new List<SecNumber>();
	}
}
