using System.Text.Json.Serialization;

namespace Fundamentalist.Common.Json
{
	public class Ticker
	{
		[JsonPropertyName("cik_str")]
		public int Cik { get; set; }
		[JsonPropertyName("ticker")]
		public string Symbol { get; set; }
		[JsonPropertyName("title")]
		public string Title { get; set; }
	}
}
