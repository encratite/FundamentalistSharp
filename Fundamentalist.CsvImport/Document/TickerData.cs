using Fundamentalist.CsvImport.Csv;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;

namespace Fundamentalist.CsvImport.Document
{
	internal class TickerData
	{
		[BsonId]
		public string Ticker { get; set; }
		public int? Cik { get; set; }
		public string Name { get; set; }
		public bool IsDelisted { get; set; }
		public string Category { get; set; }
		public string SicSector { get; set; }
		public string SicIndustry { get; set; }
		public string FamaIndustry { get; set; }
		public string Sector { get; set; }
		public string Industry { get; set; }
		public int? MarketCap { get; set; }
		public int? Revenue { get; set; }
		public string Currency { get; set; }
		public string Country { get; set; }
		public string State { get; set; }
		public List<string> RelatedTickers { get; set; } = new List<string>();

		public TickerData(TickerRow row)
		{
			Ticker = row.Ticker;
			Cik = GetInt(row.SecFilings);
			Name = row.Name;
			IsDelisted = row.IsDelisted == "Y";
			Category = row.Category;
			SicSector = row.SicSector;
			SicIndustry = row.SicIndustry;
			FamaIndustry = row.FamaIndustry;
			Sector = row.Sector;
			Industry = row.Industry;
			MarketCap = GetInt(row.MarketCap);
			Revenue = GetInt(row.Revenue);
			Currency = row.Currency;
			if (row.Location != null)
			{
				var tokens = row.Location.Split(";");
				if (tokens.Length >= 2)
				{
					Country = tokens[1].Trim();
					State = tokens[0].Trim();
				}
				else
				{
					Country = row.Location;
					State = null;
				}
			}
			if (Country == "U.S.A")
				Country = "US";
			if (row.RelatedTickers != null && row.RelatedTickers.Length > 0)
				RelatedTickers = row.RelatedTickers.Split(" ").ToList();
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
