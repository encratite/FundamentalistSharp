using System.Text.Json.Serialization;

namespace Fundamentalist.Common.Json
{
	public class CompanyFacts
	{
		[JsonConverter(typeof(NumericConverter))]
		public int Cik { get; set; }
		public Dictionary<string, Dictionary<string, Fact>> Facts { get; set; }
	}
}
