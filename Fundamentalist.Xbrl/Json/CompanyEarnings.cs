using Fundamentalist.Xblr.Json;

namespace Fundamentalist.Xblr
{
	internal class CompanyEarnings
	{
		public string Ticker { get; set; }
		public Dictionary<DateTime, Dictionary<string, FactValues>> Facts { get; set; } = new Dictionary<DateTime, Dictionary<string, FactValues>>();

		public CompanyEarnings(string ticker)
		{
			Ticker = ticker;
		}

		public override string ToString()
		{
			return $"{Ticker} ({Facts.Count} filings)";
		}
	}
}
