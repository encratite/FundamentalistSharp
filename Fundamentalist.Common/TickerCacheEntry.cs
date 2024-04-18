namespace Fundamentalist.Common
{
	public class TickerCacheEntry
	{
		public Dictionary<DateTime, float[]> Earnings { get; set; } = new Dictionary<DateTime, float[]>();
		public SortedList<DateTime, PriceData> PriceData { get; set; } = new SortedList<DateTime, PriceData>();
		public int? Index { get; set; }
	}
}
