using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Fundamentalist.Scraper
{
	internal class NullConverter<T> : ITypeConverter
	{
		Exception conversionError;
		string offendingValue;

		public Exception GetLastError()
		{
			return conversionError;
		}

		public string GetOffendingValue()
		{
			return offendingValue;
		}

		object ITypeConverter.ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			conversionError = null;
			offendingValue = null;
			try
			{
				return (T)Convert.ChangeType(text, typeof(T));
			}
			catch (Exception localConversionError)
			{
				conversionError = localConversionError;
			}
			return null;
		}

		string ITypeConverter.ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
		{
			return Convert.ToString(value);
		}
	}
}
