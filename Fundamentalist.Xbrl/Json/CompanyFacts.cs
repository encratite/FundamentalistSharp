using Fundamentalist.Xbrl;
using System.Text.Json.Serialization;

namespace Fundamentalist.Xblr.Json
{
	internal class CompanyFacts
	{
		[JsonConverter(typeof(NumericConverter))]
		public int Cik { get; set; }
		public Dictionary<string, Dictionary<string, Fact>> Facts { get; set; }
	}
}
