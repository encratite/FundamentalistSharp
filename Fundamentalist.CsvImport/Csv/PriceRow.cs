using CsvHelper.Configuration.Attributes;
using Fundamentalist.Common.Document;

namespace Fundamentalist.CsvImport.Csv
{
	internal class PriceRow
	{
		[Name("ticker")]
		public string Ticker { get; set; }
		[Name("date")]
		public DateTime Date { get; set; }
		[Name("open")]
		public decimal Open { get; set; }
		[Name("high")]
		public decimal High { get; set; }
		[Name("low")]
		public decimal Low { get; set; }
		[Name("close")]
		public decimal Close { get; set; }
		[Name("volume")]
		public decimal? Volume { get; set; }
		[Name("closeadj")]
		public decimal AdjustedClose { get; set; }
		[Name("closeunadj")]
		public decimal UnadjustedClose { get; set; }

		public Price GetPrice()
		{
			var price = new Price
			{
				Ticker = Ticker,
				Date = Date,
				Open = Open,
				High = High,
				Low = Low,
				Close = Close,
				Volume = Volume.Value,
				AdjustedClose = AdjustedClose,
				UnadjustedClose = UnadjustedClose
			};
			return price;
		}
	}
}
