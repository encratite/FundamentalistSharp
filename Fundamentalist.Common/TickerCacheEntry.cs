namespace Fundamentalist.Common
{
	public class TickerCacheEntry
	{
		public Dictionary<DateOnly, float[]> Earnings { get; set; } = new Dictionary<DateOnly, float[]>();
		public SortedList<DateOnly, PriceData> PriceData { get; set; } = new SortedList<DateOnly, PriceData>();
		public int? Index { get; set; }
	}
}
