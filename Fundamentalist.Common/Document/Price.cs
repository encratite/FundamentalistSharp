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

		[BsonIgnore]
		public decimal UnadjustedOpen
		{
			get => Ratio * Open;
		}

		[BsonIgnore]
		public decimal UnadjustedHigh
		{
			get => Ratio * High;
		}

		[BsonIgnore]
		public decimal UnadjustedLow
		{
			get => Ratio * Low;
		}

		[BsonIgnore]
		private decimal Ratio
		{
			get => UnadjustedClose.Value / Close;
		}

		public override string ToString()
		{
			return $"[{Date.ToShortDateString()}] {Ticker} {Close:F2}";
		}
	}
}
