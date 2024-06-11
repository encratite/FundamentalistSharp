using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.Common.Document
{
	public class EquitySample
	{
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime Date { get; set; }
		public decimal Value { get; set; }
	}
}
