using CsvHelper.Configuration.Attributes;

namespace Fundamentalist.CsvGenerator.Csv
{
	internal class SubRow
	{
		[Name("adsh")]
		public string AccessionNumber { get; set; }
		[Name("cik")]
		public int Cik { get; set; }
		[Name("name")]
		public string Name { get; set; }
		[Name("form")]
		public string Form { get; set; }
		[Name("period")]
		public int? PeriodInt { get; set; }
		[Name("fy")]
		public string FiscalYear { get; set; }
		[Name("fp")]
		public string FiscalPeriod { get; set; }
		[Name("filed")]
		public int FiledInt { get; set; }

		public DateOnly? Period
		{
			get => GetDate(PeriodInt);
		}

		public DateOnly? Filed
		{
			get => GetDate(FiledInt);
		}

		private DateOnly? GetDate(int? value)
		{
			if (!value.HasValue)
				return null;
			int year = value.Value / 10000;
			int month = (value.Value / 100) % 100;
			int day = value.Value % 100;
			var date = new DateOnly(year, month, day);
			return date;
		}
	}
}
