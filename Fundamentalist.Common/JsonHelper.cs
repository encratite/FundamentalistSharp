using System.Text.Json;

namespace Fundamentalist.Common
{
	public static class JsonHelper
	{
		public static T Deserialize<T>(string json)
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var output = JsonSerializer.Deserialize<T>(json, options);
			return output;
		}
	}
}
