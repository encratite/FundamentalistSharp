using CsvHelper.Configuration.Attributes;
using Fundamentalist.CsvImport.Document;

namespace Fundamentalist.CsvImport.Csv
{
	internal class NumberRow
	{
		[Name("adsh")]
		public string Adsh { get; set; }
		[Name("tag")]
		public string Tag { get; set; }
		[Name("ddate")]
		public int EndDate { get; set; }
		[Name("qtrs")]
		public int Quarters { get; set; }
		[Name("uom")]
		public string Unit { get; set; }
		[Name("value")]
		public decimal? Value { get; set; }

		public SecNumber GetSecNumber()
		{
			var output = new SecNumber
			{
				Tag = Tag,
				EndDate = IntDate.Get(EndDate).Value,
				Quarters = Quarters,
				Unit = Unit,
				Value = Value ?? 0m
			};
			return output;
		}
	}
}
