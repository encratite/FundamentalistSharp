namespace Fundamentalist.Common
{
	public class CompanyTicker
	{
		public string Company { get; set; }
		public string Ticker { get; set; }

		public static CompanyTicker GetIndexTicker()
		{
			var ticker = new CompanyTicker()
			{
				Ticker = "^GSPC",
				Company = "S&P 500"
			};
			return ticker;
		}

		public override string ToString()
		{
			return $"{Company} ({Ticker})";
		}

		public string GetJsonPath(string directory)
		{
			return GetExtensionPath(directory, "json");
		}

		public string GetCsvPath(string directory)
		{
			return GetExtensionPath(directory, "csv");
		}

		public string GetExtensionPath(string directory, string extension)
		{
			return Path.Combine(directory, $"{Ticker}.{extension}");
		}
	}
}
