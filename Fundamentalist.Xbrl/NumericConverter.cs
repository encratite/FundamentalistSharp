using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fundamentalist.Xbrl
{
	internal class NumericConverter : JsonConverter<int>
	{
		public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number)
			{
				return reader.GetInt32();
			}
			else if (reader.TokenType == JsonTokenType.String)
			{
				string input = reader.GetString();
				return int.Parse(input);
			}
			else
			{
				string input = reader.GetString();
				throw new Exception($"Unexpected value: {input}");
			}
		}

		public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}
	}
}
