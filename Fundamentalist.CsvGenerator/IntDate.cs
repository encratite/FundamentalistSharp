namespace Fundamentalist.CsvGenerator
{
	internal static class IntDate
	{
		public static DateOnly? Get(int? value)
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
