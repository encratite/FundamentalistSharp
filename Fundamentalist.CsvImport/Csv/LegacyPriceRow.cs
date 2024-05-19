using CsvHelper.Configuration.Attributes;
using Fundamentalist.Common.Document;

namespace Fundamentalist.CsvImport.Csv
{
	internal class LegacyPriceRow
	{
		[Name("Date")]
		public DateTime Date { get; set; }
		[Name("Open")]
		public decimal Open { get; set; }
		[Name("High")]
		public decimal High { get; set; }
		[Name("Low")]
		public decimal Low { get; set; }
		[Name("Close")]
		public decimal Close { get; set; }
		[Name("Adj Close")]
		public decimal AdjustedClose { get; set; }
		[Name("Volume")]
		public long Volume { get; set; }

		public Price GetPrice(string ticker)
		{
			var output = new Price
			{
				Ticker = ticker,
				Date = Date,
				Open = Open,
				High = High,
				Low = Low,
				Close = Close,
				Volume = Volume,
				AdjustedClose = AdjustedClose,
				UnadjustedClose = null
			};
			return output;
		}
	}
}
