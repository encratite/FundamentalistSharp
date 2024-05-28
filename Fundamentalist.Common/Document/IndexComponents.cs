using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.Common.Document
{
	public class IndexComponents
	{
		[BsonId]
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime Date { get; set; }
		public List<string> Tickers { get; set; }
	}
}
