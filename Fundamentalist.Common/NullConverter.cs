using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Fundamentalist.Common
{
	internal class NullConverter<T> : ITypeConverter
	{
		private Exception _conversionError;
		private string _offendingValue;

		public Exception GetLastError()
		{
			return _conversionError;
		}

		public string GetOffendingValue()
		{
			return _offendingValue;
		}

		object ITypeConverter.ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			_conversionError = null;
			_offendingValue = null;
			try
			{
				return (T)Convert.ChangeType(text, typeof(T));
			}
			catch (Exception localConversionError)
			{
				_conversionError = localConversionError;
			}
			return null;
		}

		string ITypeConverter.ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
		{
			return Convert.ToString(value);
		}
	}
}
