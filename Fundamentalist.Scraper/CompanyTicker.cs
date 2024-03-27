namespace Fundamentalist.Scraper
{
	internal class CompanyTicker
	{
		public string Company { get; set; }
		public string Ticker { get; set; }

		public override string ToString()
		{
			return $"{Company} ({Ticker})";
		}

		public string GetJsonPath(string directory)
		{
			return Path.Combine(directory, $"{Ticker}.json");
		}
	}
}
