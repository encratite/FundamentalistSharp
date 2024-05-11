using Fundamentalist.Common;

namespace Fundamentalist.LeadLag
{
	internal class TickerData
	{
		public string Name { get; set; }
		public SortedList<DateOnly, PriceData> PriceData { get; set; }

		public TickerData(string name, SortedList<DateOnly, PriceData> priceData)
		{
			Name = name;
			PriceData = priceData;
		}
	}
}
