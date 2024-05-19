using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.Common.Document
{
	public class Price
	{
		public ObjectId Id { get; set; }
		public string Ticker { get; set; }
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime Date { get; set; }
		public decimal Open { get; set; }
		public decimal High { get; set; }
		public decimal Low { get; set; }
		public decimal Close { get; set; }
		public decimal Volume { get; set; }
		public decimal AdjustedClose { get; set; }
		public decimal? UnadjustedClose { get; set; }
	}
}
