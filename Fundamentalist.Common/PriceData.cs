namespace Fundamentalist.Common
{
	public class PriceData
	{
		public DateOnly Date { get; set; }
		public decimal Open { get; set; }
		public decimal High { get; set; }
		public decimal Low { get; set; }
		public decimal Close { get; set; }
		public decimal AdjustedClose { get; set; }
		public long Volume { get; set; }

		public override string ToString()
		{
			return $"{Date.ToShortDateString()} {Close}";
		}
	}
}
