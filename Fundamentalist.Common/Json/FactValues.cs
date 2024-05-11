using System.Text.Json.Serialization;

namespace Fundamentalist.Common.Json
{
	public class FactValues
	{
		public DateOnly Start { get; set; }
		public DateOnly End { get; set; }
		[JsonPropertyName("val")]
		public decimal Value { get; set; }
		[JsonPropertyName("accn")]
		public string Account { get; set; }
		[JsonPropertyName("fy")]
		public int? FiscalYear { get; set; }
		[JsonPropertyName("fp")]
		public string FiscalPeriod { get; set; }
		public string Form { get; set; }
		public DateOnly Filed { get; set; }
		public string Frame { get; set; }

		public override string ToString()
		{
			return $"{Value} ({Form} {Filed.ToShortDateString()})";
		}
	}
}
