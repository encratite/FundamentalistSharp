using System.Text.Json.Serialization;

namespace Fundamentalist.CsvGenerator
{
	internal class MarketCapSample
	{
		[JsonPropertyName("d")]
		public long Timestamp { get; set; }
		[JsonPropertyName("m")]
		public int MarketCap { get; set; }
	}
}
