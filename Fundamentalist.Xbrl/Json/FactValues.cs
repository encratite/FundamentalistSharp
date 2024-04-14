using NetJSON;
using System.Text.Json.Serialization;

namespace Fundamentalist.Xblr.Json
{
	internal class FactValues
	{
		public DateTime End { get; set; }
		[JsonPropertyName("val")]
		[NetJSONProperty("val")]
		public decimal Value { get; set; }
		[JsonPropertyName("accn")]
		[NetJSONProperty("accn")]
		public string Account { get; set; }
		[JsonPropertyName("fy")]
		[NetJSONProperty("fy")]
		public int? FiscalYear { get; set; }
		[JsonPropertyName("fp")]
		[NetJSONProperty("fp")]
		public string FiscalPeriod { get; set; }
		public string Form { get; set; }
		public DateTime Filed { get; set; }
		public string Frame { get; set; }

		public override string ToString()
		{
			if (Form == "10-K")
				return $"{Value} ({Form} {End.Year})";
			else if (Form == "10-Q")
			{
				var map = new Dictionary<int, string>
				{
					{ 2, "Q1" },
					{ 5, "Q2" },
					{ 8, "Q3" },
					{ 11, "Q4" }
				};
				string description;
				if (!map.TryGetValue(End.Month, out description))
					description = "Q?";
				return $"{Value} ({Form} {End.Year} {description})";
			}
			else
				return $"{Value} ({Form} {End.ToShortDateString()})";
		}
	}
}
