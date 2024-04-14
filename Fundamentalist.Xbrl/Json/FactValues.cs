using System.Text.Json.Serialization;

namespace Fundamentalist.Xblr.Json
{
	internal class FactValues
	{
		public DateTime End { get; set; }
		[JsonPropertyName("val")]
		public decimal Value { get; set; }
		[JsonPropertyName("accn")]
		public string Account { get; set; }
		[JsonPropertyName("fy")]
		public int? FiscalYear { get; set; }
		[JsonPropertyName("fp")]
		public string FiscalPeriod { get; set; }
		public string Form { get; set; }
		public DateTime Filed { get; set; }
		public string Frame { get; set; }

		public override string ToString()
		{
			if (Form == "10-K")
				return $"{Value} ({Form} {End.Year})";
			else if (Form == "10-Q")
				return $"{Value} ({Form} {End.Year} {FiscalPeriod})";
			else
				return $"{Value} ({Form} {End.ToShortDateString()})";
		}
	}
}
