using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.Common.Document
{
	public class CorporateAction
	{
		public ObjectId Id { get; set; }
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime Date { get; set; }
		public string Action { get; set; }
		public string Ticker { get; set; }
		public string Name { get; set; }
		public decimal? Value { get; set; }
		public string ContraTicker { get; set; }
		public string ContraName { get; set; }
	}
}
