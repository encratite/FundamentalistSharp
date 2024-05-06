namespace Fundamentalist.SqlAnalysis
{
	internal class OpenClosePrice
	{
		public decimal Open { get; set; }
		public decimal Close { get; set; }

		public OpenClosePrice(decimal open, decimal close)
		{
			Open = open;
			Close = close;
		}
	}
}
