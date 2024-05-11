namespace Fundamentalist.Common
{
	public class EarningsLine
	{
		public string Ticker { get; set; }
		public DateOnly Date { get; set; }
		public float[] Features { get; set; }
	}
}
