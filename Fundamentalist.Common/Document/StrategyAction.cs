using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Fundamentalist.Common.Document
{
	public enum StrategyActionEnum
	{
		Buy,
		Sell
	}

	public class StrategyAction
	{
		public DateTime Time { get; set; }
		[BsonRepresentation(BsonType.String)]
		public StrategyActionEnum Action { get; set; }
		public string Ticker { get; set; }
		public decimal Price { get; set; }
		public long Count { get; set; }
	}
}
