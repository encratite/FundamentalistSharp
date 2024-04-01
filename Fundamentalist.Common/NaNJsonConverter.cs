using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fundamentalist.Common
{
	public class NaNJsonConverter : JsonConverter<decimal?>
	{
		public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number)
				return reader.GetDecimal();
			else
				return null;
		}

		public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}
	}
}
