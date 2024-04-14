using System.Text.Json.Serialization;

namespace Fundamentalist.Xbrl.Json
{
	internal class Ticker
	{
		[JsonPropertyName("cik_str")]
		public int Cik { get; set; }
		[JsonPropertyName("ticker")]
		public string Symbol { get; set; }
		public string Title { get; set; }
	}
}
