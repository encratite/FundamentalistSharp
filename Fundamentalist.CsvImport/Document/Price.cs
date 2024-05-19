using Fundamentalist.CsvImport.Csv;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.CsvImport.Document
{
	internal class Price
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
		public decimal UnadjustedClose { get; set; }

		public Price(PriceRow row)
		{
			Ticker = row.Ticker;
			Date = row.Date;
			Open = row.Open;
			High = row.High;
			Low = row.Low;
			Close = row.Close;
			Volume = row.Volume.Value;
			AdjustedClose = row.AdjustedClose;
			UnadjustedClose = row.UnadjustedClose;
		}
	}
}
