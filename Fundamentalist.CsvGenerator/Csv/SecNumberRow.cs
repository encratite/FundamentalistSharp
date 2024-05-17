using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class SecNumberRow
	{
		[Name("adsh")]
		public string Adsh { get; set; }
		[Name("tag")]
		public string Tag { get; set; }
		[Name("end_date")]
		public DateOnly EndDate { get; set; }
		[Name("quarters")]
		public int Quarters { get; set; }
		[Name("unit")]
		public string Unit { get; set; }
		[Name("value")]
		public decimal Value { get; set; }

		public SecNumberRow(NumberRow number)
		{
			Adsh = number.Adsh;
			Tag = number.Tag;
			EndDate = IntDate.Get(number.EndDate).Value;
			Quarters = number.Quarters;
			Unit = number.Unit;
			Value = number.Value ?? 0m;
		}
	}
}
