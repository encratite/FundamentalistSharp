using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Fundamentalist.CsvImport
{
	internal class PriceDecimalConverter : DecimalConverter
	{
		public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			if (text == "")
				return null;
			if (decimal.TryParse(text, out var result))
			{
				return result;
			}
			else
			{
				return (decimal)double.Parse(text);
			}
		}
	}
}
