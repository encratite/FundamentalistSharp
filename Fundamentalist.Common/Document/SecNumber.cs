using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.CsvImport.Document
{
	public class SecNumber
	{
		public string Tag { get; set; }
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime EndDate { get; set; }
		public int Quarters { get; set; }
		public string Unit { get; set; }
		public decimal Value { get; set; }
	}
}
