﻿using Fundamentalist.Common.Json;

namespace Fundamentalist.Common
{
	public class CompanyEarnings
	{
		public Ticker Ticker { get; set; }
		public Dictionary<DateTime, Dictionary<string, FactValues>> Facts { get; set; } = new Dictionary<DateTime, Dictionary<string, FactValues>>();

		public CompanyEarnings(Ticker ticker)
		{
			Ticker = ticker;
		}

		public override string ToString()
		{
			return $"{Ticker.Symbol} ({Facts.Count} filings)";
		}
	}
}
