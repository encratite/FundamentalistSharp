﻿using Fundamentalist.Common;

namespace Fundamentalist.LeadLag
{
	internal class TickerData
	{
		public string Name { get; set; }
		public SortedList<DateTime, PriceData> PriceData { get; set; }

		public TickerData(string name, SortedList<DateTime, PriceData> priceData)
		{
			Name = name;
			PriceData = priceData;
		}
	}
}
