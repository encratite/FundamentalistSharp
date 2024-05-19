using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.CsvImport.Document
{
	public class TickerData
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
	}
}
