using CsvHelper.Configuration.Attributes;
using Fundamentalist.CsvImport.Document;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;

namespace Fundamentalist.CsvImport.Csv
{
	internal class TickerRow
	{
		[Name("ticker")]
		public string Ticker { get; set; }
		[Name("name")]
		public string Name { get; set; }
		[Name("isdelisted")]
		public string IsDelisted { get; set; }
		[Name("category")]
		public string Category { get; set; }
		[Name("sicsector")]
		public string SicSector { get; set; }
		[Name("sicindustry")]
		public string SicIndustry { get; set; }
		[Name("famaindustry")]
		public string FamaIndustry { get; set; }
		[Name("sector")]
		public string Sector { get; set; }
		[Name("industry")]
		public string Industry { get; set; }
		[Name("scalemarketcap")]
		public string MarketCap { get; set; }
		[Name("scalerevenue")]
		public string Revenue { get; set; }
		[Name("relatedtickers")]
		public string RelatedTickers { get; set; }
		[Name("currency")]
		public string Currency { get; set; }
		[Name("location")]
		public string Location { get; set; }
		[Name("secfilings")]
		public string SecFilings { get; set; }

		public TickerData GetTickerData()
		{
			var output = new TickerData
			{
				Ticker = Ticker,
				Cik = GetInt(SecFilings),
				Name = Name,
				IsDelisted = IsDelisted == "Y",
				Category = Category,
				SicSector = SicSector,
				SicIndustry = SicIndustry,
				FamaIndustry = FamaIndustry,
				Sector = Sector,
				Industry = Industry,
				MarketCap = GetInt(MarketCap),
				Revenue = GetInt(Revenue),
				Currency = Currency
			};
			if (Location != null)
			{
				var tokens = Location.Split(";");
				if (tokens.Length >= 2)
				{
					output.Country = tokens[1].Trim();
					output.State = tokens[0].Trim();
				}
				else
				{
					output.Country = Location;
					output.State = null;
				}
			}
			if (output.Country == "U.S.A")
				output.Country = "US";
			if (RelatedTickers != null && RelatedTickers.Length > 0)
				output.RelatedTickers = RelatedTickers.Split(" ").ToList();
			return output;
		}

		private int? GetInt(string input)
		{
			var numberPattern = new Regex("[1-9][0-9]*");
			var match = numberPattern.Match(input);
			if (match.Success)
				return int.Parse(match.ToString());
			else
				return null;
		}
	}
}